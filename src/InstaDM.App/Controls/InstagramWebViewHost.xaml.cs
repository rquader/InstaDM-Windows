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
    private readonly NavigationPolicy _policy = new();
    private readonly WebViewHostConfiguration _configuration;
    private bool _initialized;

    /// <summary>Raised with a schema-validated guard report. The recovery
    /// coordinator (M8) subscribes; nothing else consumes web messages.</summary>
    public event EventHandler<GuardMessage>? GuardReported;

    public InstagramWebViewHost()
    {
        InitializeComponent();

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

        // 3. Document-start guard, from the single C# policy source. Must be
        //    registered before any navigation so it beats Instagram's bundles.
        var guardTemplate = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Web", "containment-guard.js"));
        await core.AddScriptToExecuteOnDocumentCreatedAsync(
            PolicyScriptBuilder.InjectIntoScript(guardTemplate, _policy));

        // 4. Event wiring — each handler enforces one named invariant.
        core.NavigationStarting += OnNavigationStarting;
        core.FrameNavigationStarting += OnFrameNavigationStarting;
        core.NewWindowRequested += OnNewWindowRequested;
        core.PermissionRequested += OnPermissionRequested;
        core.DownloadStarting += OnDownloadStarting;
        core.ProcessFailed += OnProcessFailed;
        core.WebMessageReceived += OnWebMessageReceived;

        // 5. First navigation, only now.
        core.Navigate(NavigationPolicy.InboxUrl);
    }

    /// <summary>Invariant: no disallowed URL becomes the top-level document.
    /// Uses the network-request layer (auth surfaces and DM routes pass);
    /// blocked incidental hops are cancelled without any further reaction —
    /// stop/reload here is what broke thread scroll history on macOS.</summary>
    private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        var decision = _policy.DecideNetworkRequest(args.Uri);
        if (!decision.IsAllowed)
        {
            args.Cancel = true;
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

    /// <summary>Invariant: a crashed renderer recovers to the inbox instead
    /// of leaving a dead pane; repeated process failure surfaces an honest
    /// error instead of a reload loop. Counter resets on the next successful
    /// recovery navigation (wired below in <see cref="OnRecoveryCompleted"/>).</summary>
    private int _processFailures;

    private void OnProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs args)
    {
        _processFailures++;
        if (_processFailures <= 3)
        {
            RuntimeFailureBar.IsOpen = true;
            sender.NavigationCompleted += OnRecoveryCompleted;
            sender.Navigate(NavigationPolicy.InboxUrl);
        }
        else
        {
            RuntimeFailureBar.Message =
                "The embedded browser keeps failing. Close and reopen the app.";
            RuntimeFailureBar.IsOpen = true;
        }
    }

    private void OnRecoveryCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        sender.NavigationCompleted -= OnRecoveryCompleted;
        if (args.IsSuccess)
        {
            _processFailures = 0;
            RuntimeFailureBar.IsOpen = false;
        }
    }

    /// <summary>Invariant: web messages are untrusted; only exact-schema
    /// guard reports pass, as validated types — raw JSON never propagates.</summary>
    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (GuardMessage.TryParse(args.WebMessageAsJson, out var message))
        {
            GuardReported?.Invoke(this, message);
        }
    }

    /// <summary>Clears all Instagram data in the dedicated profile: cookies,
    /// cache, storage, everything. Used by the sign-out flow (M9) and never
    /// touches any other browser or system profile.</summary>
    public async Task ClearInstagramDataAsync()
    {
        var core = WebView.CoreWebView2;
        if (core is null)
        {
            return;
        }

        await core.Profile.ClearBrowsingDataAsync();
        core.Navigate(NavigationPolicy.InboxUrl); // Instagram will show login
    }
}
