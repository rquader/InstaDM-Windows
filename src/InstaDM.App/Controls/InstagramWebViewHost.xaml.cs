using InstaDM.App.Services;
using InstaDM.Core.Authentication;
using InstaDM.Core.Navigation;
using InstaDM.Core.WebHost;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace InstaDM.App.Controls;

/// <summary>
/// The privacy-hardened WebView2 host for Instagram's web client.
///
/// Initialization order is load-bearing and must not be reshuffled:
/// environment (privacy switches that can never be changed at runtime) →
/// settings (before any page exists) → document-start guard registration →
/// event wiring → only then the first navigation. Every event handler below
/// documents the single invariant it enforces.
///
/// PRIVACY: this class never reads cookie values, page content, or message
/// text. Diagnostics are coarse category enums, Debug-only.
/// </summary>
public sealed partial class InstagramWebViewHost : UserControl
{
    private readonly NavigationPolicy _policy;
    private readonly NavigationRecoveryCoordinator _recovery;
    private readonly WebViewHostConfiguration _configuration;
    private readonly AuthenticationStateMachine _auth = new();
    private AuthSessionWatcher? _sessionWatcher;
    private bool _initialized;

    /// <summary>Raised with a schema-validated guard report after the recovery
    /// coordinator has already absorbed it. External subscribers are optional.</summary>
    public event EventHandler<GuardMessage>? GuardReported;

    public InstagramWebViewHost()
    {
        InitializeComponent();
        _policy = new NavigationPolicy(App.Current.CreatePolicyOptions());
        _recovery = new NavigationRecoveryCoordinator(_policy);

        _configuration = WebViewHostConfiguration.Create(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
#if DEBUG
            isDebugBuild: true
#else
            isDebugBuild: false
#endif
        );

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;

        await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        // 1. Environment: switches here are immutable for the process
        //    lifetime — SmartScreen and Chromium background services are
        //    disabled at a level page or runtime code cannot undo.
        var environmentOptions = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = _configuration.AdditionalBrowserArguments,
            AllowSingleSignOnUsingOSPrimaryAccount =
                _configuration.AllowSingleSignOnUsingOSPrimaryAccount,
            AreBrowserExtensionsEnabled = _configuration.AreBrowserExtensionsEnabled,
            IsCustomCrashReportingEnabled = _configuration.IsCustomCrashReportingEnabled,
        };

        var environment = await CoreWebView2Environment.CreateWithOptionsAsync(
            browserExecutableFolder: null,
            userDataFolder: _configuration.UserDataFolder,
            options: environmentOptions);

        await WebView.EnsureCoreWebView2Async(environment);
        var core = WebView.CoreWebView2;

        // 2. Settings — applied while about:blank, before Instagram exists.
        var settings = core.Settings;
        settings.IsReputationCheckingRequired = _configuration.IsReputationCheckingRequired;
        settings.IsPasswordAutosaveEnabled = _configuration.IsPasswordAutosaveEnabled;
        settings.IsGeneralAutofillEnabled = _configuration.IsGeneralAutofillEnabled;
        settings.AreDevToolsEnabled = _configuration.AreDevToolsEnabled;
        settings.IsStatusBarEnabled = _configuration.IsStatusBarEnabled;
        settings.AreBrowserAcceleratorKeysEnabled =
            _configuration.AreBrowserAcceleratorKeysEnabled;
        settings.AreDefaultContextMenusEnabled = true; // copy/paste in chat is core UX
        settings.IsZoomControlEnabled = true;          // accessibility

        core.Profile.PreferredTrackingPreventionLevel =
            CoreWebView2TrackingPreventionLevel.Balanced;

        // 3. Document-start guard + cosmetic shell. Guard must precede
        //    Instagram's bundles; CSS is presentation-only (not containment).
        var guardTemplate = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Web", "containment-guard.js"));
        await core.AddScriptToExecuteOnDocumentCreatedAsync(
            PolicyScriptBuilder.InjectIntoScript(guardTemplate, _policy));

        var cosmeticCss = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Web", "cosmetic-shell.css"));
        var cosmeticJs =
            "(function(){var s=document.createElement('style');s.textContent=" +
            System.Text.Json.JsonSerializer.Serialize(cosmeticCss) +
            ";document.documentElement.appendChild(s);})();";
        await core.AddScriptToExecuteOnDocumentCreatedAsync(cosmeticJs);

        // 4. Event wiring — each handler enforces one named invariant.
        core.NavigationStarting += OnNavigationStarting;
        core.FrameNavigationStarting += OnFrameNavigationStarting;
        core.NewWindowRequested += OnNewWindowRequested;
        core.PermissionRequested += OnPermissionRequested;
        core.DownloadStarting += OnDownloadStarting;
        core.ProcessFailed += OnProcessFailed;
        core.WebMessageReceived += OnWebMessageReceived;
        core.NavigationCompleted += OnNavigationCompleted;

        // Authentication: cookie EXISTENCE + committed surface category are
        // the only inputs; the pure state machine decides everything.
        _sessionWatcher = new AuthSessionWatcher(
            new WebViewSessionCookieProbe(core, DispatcherQueue));
        App.Current.Lifecycle.Own(_sessionWatcher);
        Unloaded += OnUnloaded;

        // 5. First navigation, only now. Assume absent until the probe
        //    reports otherwise — the watcher corrects within one interval
        //    and never reads cookie values (docs/SOURCE_BEHAVIOR.md B5).
        core.Navigate(NavigationPolicy.InboxUrl);
        HandleAuthEvent(AuthenticationEvent.SessionCookieAbsent);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _sessionWatcher?.Stop();
        _sessionWatcher?.Dispose();
        _sessionWatcher = null;
    }

    // ------------------------------------------------------------------
    // Authentication wiring
    // ------------------------------------------------------------------

    /// <summary>Feeds an event to the state machine and executes the single
    /// action it returns. Always called on the dispatcher thread.</summary>
    private void HandleAuthEvent(AuthenticationEvent evt)
    {
        var core = WebView.CoreWebView2;
        if (core is null)
        {
            return;
        }

        switch (_auth.Handle(evt))
        {
            case AuthenticationStateMachine.Action.NavigateToInbox:
                core.Navigate(NavigationPolicy.InboxUrl);
                break;

            case AuthenticationStateMachine.Action.StartCookieWatch:
                _sessionWatcher?.Start(exists => DispatcherQueue.TryEnqueue(() =>
                    HandleAuthEvent(exists
                        ? AuthenticationEvent.SessionCookiePresent
                        : AuthenticationEvent.SessionCookieAbsent)));
                break;

            case AuthenticationStateMachine.Action.StopCookieWatch:
                _sessionWatcher?.Stop();
                break;

            case AuthenticationStateMachine.Action.ShowFatalError:
                RuntimeFailureBar.Message =
                    "The embedded browser keeps failing. Close and reopen the app.";
                RuntimeFailureBar.IsOpen = true;
                break;
        }
    }

    /// <summary>Invariant: the state machine learns which surface CATEGORY
    /// committed (auth / challenge / DM) — never the URL itself. Successful
    /// completion while Recovering also closes the process-failure budget.
    /// Recovery then records last-valid DM or rebounds off-policy commits
    /// without calling Stop()/reload.</summary>
    private void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess)
        {
            return;
        }

        if (_auth.State == AuthenticationState.Recovering)
        {
            RuntimeFailureBar.IsOpen = false;
            HandleAuthEvent(AuthenticationEvent.RecoveryNavigationSucceeded);
            // Fall through: the same commit may also be a DM/auth surface.
        }

        switch (_policy.Classify(sender.Source))
        {
            case InstagramSurface.AuthAccount:
            case InstagramSurface.AuthHost:
                HandleAuthEvent(AuthenticationEvent.AuthSurfaceCommitted);
                break;
            case InstagramSurface.AuthChallenge:
            case InstagramSurface.AuthPlatform:
                HandleAuthEvent(AuthenticationEvent.ChallengeSurfaceCommitted);
                break;
            case InstagramSurface.DirectInbox:
            case InstagramSurface.DirectThread:
            case InstagramSurface.DirectNew:
                HandleAuthEvent(AuthenticationEvent.DirectSurfaceCommitted);
                break;
        }

        ApplyRecovery(sender, _recovery.OnMainDocumentCommitted(sender.Source));
    }

    /// <summary>Invariant: disallowed top-level navigations are cancelled
    /// here only — never Stop()/reload (that aborted pagination XHR on
    /// macOS). User escapes rebound to the last valid DM; incidental hops
    /// are silent. Same-thread re-nav after settle is suppressed.</summary>
    private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        var decision = _recovery.OnNavigationStarting(args.Uri, MapInitiator(args));
        ApplyRecovery(sender, decision, args);
    }

    private static NavigationInitiator MapInitiator(CoreWebView2NavigationStartingEventArgs args) =>
        args.IsUserInitiated
            ? NavigationInitiator.UserActivated
            : NavigationInitiator.Other;

    /// <summary>Executes a recovery decision. Never calls Stop().</summary>
    private static void ApplyRecovery(
        CoreWebView2 core,
        RecoveryDecision decision,
        CoreWebView2NavigationStartingEventArgs? starting = null)
    {
        switch (decision.Action)
        {
            case RecoveryActionKind.None:
                break;

            case RecoveryActionKind.CancelSilent:
            case RecoveryActionKind.CancelSameThread:
                if (starting is not null)
                {
                    starting.Cancel = true;
                }

                break;

            case RecoveryActionKind.CancelAndRebound:
                if (starting is not null)
                {
                    starting.Cancel = true;
                }

                if (decision.ReboundUrl is not null)
                {
                    core.Navigate(decision.ReboundUrl);
                }

                break;

            case RecoveryActionKind.Rebound:
                if (decision.ReboundUrl is not null)
                {
                    core.Navigate(decision.ReboundUrl);
                }

                break;
        }
    }

    /// <summary>Invariant: frames may not load off-platform documents.
    /// Instagram's own subresource frames pass the same request policy.</summary>
    private void OnFrameNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (!_policy.DecideNetworkRequest(args.Uri).IsAllowed)
        {
            args.Cancel = true;
        }
    }

    /// <summary>Invariant: popups can never bypass the top-level policy.
    /// Allowed destinations open in this same view; everything else is
    /// dropped. No second window ever hosts the session.</summary>
    private void OnNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        args.Handled = true;
        if (_policy.IsUserSurface(args.Uri))
        {
            sender.Navigate(args.Uri);
        }
    }

    /// <summary>Invariant: default-deny every capability. A future
    /// user-initiated feature (e.g. clipboard) would be granted narrowly
    /// here after privacy review, per docs/PRIVACY_THREAT_MODEL.md.</summary>
    private void OnPermissionRequested(CoreWebView2 sender, CoreWebView2PermissionRequestedEventArgs args)
    {
        args.State = CoreWebView2PermissionState.Deny;
        args.Handled = true;
    }

    /// <summary>Invariant: no unexpected downloads. A deliberate local save
    /// flow may arrive later; until then everything is cancelled silently.</summary>
    private void OnDownloadStarting(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs args)
    {
        args.Cancel = true;
        args.Handled = true;
    }

    /// <summary>Invariant: a crashed renderer recovers through the auth
    /// state machine (capped retries, then fatal). No parallel failure
    /// counter — the machine owns the budget.</summary>
    private void OnProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs args)
    {
        _sessionWatcher?.Stop();
        RuntimeFailureBar.IsOpen = true;
        RuntimeFailureBar.Message = "Reloading the messaging view…";
        HandleAuthEvent(AuthenticationEvent.WebProcessFailed);
    }

    /// <summary>Invariant: web messages are untrusted; only exact-schema
    /// guard reports pass. Blocked SPA transitions are absorbed by recovery
    /// without rebound (the page never left the DM document).</summary>
    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (!GuardMessage.TryParse(args.WebMessageAsJson, out var message))
        {
            return;
        }

        _recovery.OnGuardBlocked(ToInstagramSurface(message.Surface));
        GuardReported?.Invoke(this, message);
    }

    private static InstagramSurface ToInstagramSurface(GuardReportedSurface surface) =>
        surface switch
        {
            GuardReportedSurface.Malformed => InstagramSurface.Malformed,
            GuardReportedSurface.OffPlatform => InstagramSurface.OffPlatform,
            GuardReportedSurface.Feed => InstagramSurface.HomeFeed,
            GuardReportedSurface.Explore => InstagramSurface.Explore,
            GuardReportedSurface.Reels => InstagramSurface.Reels,
            GuardReportedSurface.Stories => InstagramSurface.Stories,
            GuardReportedSurface.Post => InstagramSurface.Post,
            GuardReportedSurface.DirectShell => InstagramSurface.DirectShell,
            _ => InstagramSurface.UnknownInstagram,
        };

    /// <summary>Optional Follow Requests surface. Only called when the
    /// setting is enabled; native policy still gates the path.</summary>
    public void NavigateToFollowRequests()
    {
        WebView.CoreWebView2?.Navigate(NavigationPolicy.FollowRequestsUrl);
    }

    /// <summary>Clears all Instagram data in the dedicated profile: cookies,
    /// cache, storage, everything. Used by the sign-out flow (M9) and never
    /// touches any other browser or system profile. Stops pollers first so a
    /// mid-clear cookie observation cannot race the state machine.</summary>
    public async Task ClearInstagramDataAsync()
    {
        var core = WebView.CoreWebView2;
        if (core is null)
        {
            return;
        }

        _sessionWatcher?.Stop();
        _recovery.Reset();
        await core.Profile.ClearBrowsingDataAsync();
        HandleAuthEvent(AuthenticationEvent.DataCleared);
        core.Navigate(NavigationPolicy.InboxUrl); // Instagram will show login
    }
}
