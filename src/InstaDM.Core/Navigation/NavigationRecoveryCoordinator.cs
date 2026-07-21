namespace InstaDM.Core.Navigation;

/// <summary>Who/what triggered a navigation attempt. Mapped from WebView2's
/// user-initiated flag and navigation kind by the host adapter.</summary>
public enum NavigationInitiator
{
    /// <summary>Explicit user gesture (link click, address bar, etc.).</summary>
    UserActivated,

    /// <summary>HTTP redirect or similar committed transition.</summary>
    Redirect,

    /// <summary>Background/prefetch/script-driven hop. Treat as incidental
    /// unless the committed document lands off a DM surface.</summary>
    Other,
}

/// <summary>What the host must do after the coordinator judges an event.
/// Deliberately excludes <c>Stop()</c>/<c>stopLoading()</c> — those abort
/// pagination XHR and snap scroll (docs/SOURCE_BEHAVIOR.md B7).</summary>
public enum RecoveryActionKind
{
    /// <summary>Allow the navigation / ignore the event.</summary>
    None,

    /// <summary>Cancel the in-flight navigation only. No reload, no rebound.</summary>
    CancelSilent,

    /// <summary>Cancel a same-thread re-navigation that would snap scroll.</summary>
    CancelSameThread,

    /// <summary>Cancel the escape and navigate back to the last valid DM URL.</summary>
    CancelAndRebound,

    /// <summary>The main document already landed off-policy; rebound now.</summary>
    Rebound,
}

/// <summary>Privacy-safe recovery outcome: action + coarse surface, never a
/// raw URL in diagnostics.</summary>
public sealed record RecoveryDecision(
    RecoveryActionKind Action,
    InstagramSurface Surface,
    string? ReboundUrl)
{
    public static RecoveryDecision Allow(InstagramSurface surface) =>
        new(RecoveryActionKind.None, surface, null);

    public static RecoveryDecision CancelSilent(InstagramSurface surface) =>
        new(RecoveryActionKind.CancelSilent, surface, null);

    public static RecoveryDecision CancelSameThread(InstagramSurface surface) =>
        new(RecoveryActionKind.CancelSameThread, surface, null);

    public static RecoveryDecision CancelAndRebound(InstagramSurface surface, string reboundUrl) =>
        new(RecoveryActionKind.CancelAndRebound, surface, reboundUrl);

    public static RecoveryDecision ReboundTo(InstagramSurface surface, string reboundUrl) =>
        new(RecoveryActionKind.Rebound, surface, reboundUrl);
}

/// <summary>
/// Pure navigation recovery ladder. Owns last-valid-DM memory, settled-state,
/// same-thread suppression, bounce cooldown, and loop caps. The WebView host
/// translates platform events into these methods and executes the returned
/// action — never calling Stop()/reload for incidental blocks.
/// </summary>
public sealed class NavigationRecoveryCoordinator
{
    /// <summary>Minimum gap between rebound navigations.</summary>
    public static readonly TimeSpan BounceCooldown = TimeSpan.FromMilliseconds(750);

    /// <summary>Consecutive rebounds allowed inside <see cref="LoopWindow"/>
    /// before failing closed to the inbox (nil-URL / 302 bounce loops).</summary>
    public const int MaxReboundsPerWindow = 5;

    /// <summary>Sliding window used with <see cref="MaxReboundsPerWindow"/>.</summary>
    public static readonly TimeSpan LoopWindow = TimeSpan.FromSeconds(5);

    private readonly NavigationPolicy _policy;
    private readonly Func<DateTimeOffset> _utcNow;

    private string _lastValidDmUrl = NavigationPolicy.InboxUrl;
    private string? _settledThreadKey;
    private bool _hasSettledOnUserSurface;
    private DateTimeOffset _lastReboundAt = DateTimeOffset.MinValue;
    private readonly Queue<DateTimeOffset> _recentRebounds = new();

    public NavigationRecoveryCoordinator(
        NavigationPolicy? policy = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        _policy = policy ?? new NavigationPolicy();
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>Last DM surface the user successfully settled on. Always a
    /// canonical https Instagram DM URL; defaults to the inbox.</summary>
    public string LastValidDmUrl => _lastValidDmUrl;

    /// <summary>True after the first successful commit of a user surface
    /// (DM or allowed optional). Same-thread suppression is off until then
    /// so cold launch cannot white-screen.</summary>
    public bool HasSettledOnUserSurface => _hasSettledOnUserSurface;

    /// <summary>Call when a main-frame navigation is about to start.
    /// Returning Cancel* means the host sets <c>args.Cancel = true</c> and
    /// does nothing else for silent/same-thread cases.</summary>
    public RecoveryDecision OnNavigationStarting(string? rawUrl, NavigationInitiator initiator)
    {
        var surface = _policy.Classify(rawUrl);
        var network = _policy.DecideNetworkRequest(rawUrl);

        if (network.IsAllowed)
        {
            // Same-thread re-entry after settle snaps scroll — suppress only
            // post-settle, never during cold launch.
            if (_hasSettledOnUserSurface
                && initiator != NavigationInitiator.UserActivated
                && IsSameSettledThread(rawUrl))
            {
                return RecoveryDecision.CancelSameThread(surface);
            }

            return RecoveryDecision.Allow(surface);
        }

        // Blocked path. Background/prefetch/redirect hops: cancel only —
        // never rebound (heal-thrash H1). Explicit user escapes rebound.
        if (initiator != NavigationInitiator.UserActivated)
        {
            return RecoveryDecision.CancelSilent(surface);
        }

        return TryRebound(surface, cancelFirst: true);
    }

    /// <summary>Call when a main document finishes loading successfully.
    /// Records last-valid DM; rebounds if Instagram managed to commit an
    /// off-policy document despite earlier cancellation.</summary>
    public RecoveryDecision OnMainDocumentCommitted(string? rawUrl)
    {
        var surface = _policy.Classify(rawUrl);

        if (UrlCanonicalizer.TryCanonicalize(rawUrl, out var canonical)
            && NavigationPolicy.IsDirectMessagingPath(canonical.Path)
            && _policy.IsUserSurface(rawUrl))
        {
            _lastValidDmUrl = ToAbsoluteUrl(canonical);
            _settledThreadKey = ThreadKey(canonical.Path);
            _hasSettledOnUserSurface = true;
            return RecoveryDecision.Allow(surface);
        }

        if (_policy.IsUserSurface(rawUrl))
        {
            // Auth / optional allowed surfaces: settle without updating last DM.
            _hasSettledOnUserSurface = true;
            return RecoveryDecision.Allow(surface);
        }

        if (_policy.ShouldRecoverFromMainDocument(rawUrl))
        {
            return TryRebound(surface, cancelFirst: false);
        }

        return RecoveryDecision.Allow(surface);
    }

    /// <summary>Guard reported a blocked SPA transition. The page never left
    /// the DM document, so no rebound — keep last-valid as-is.</summary>
    public RecoveryDecision OnGuardBlocked(InstagramSurface surface) =>
        RecoveryDecision.CancelSilent(surface);

    /// <summary>Reset in-memory recovery state (clear-data / fresh profile).</summary>
    public void Reset()
    {
        _lastValidDmUrl = NavigationPolicy.InboxUrl;
        _settledThreadKey = null;
        _hasSettledOnUserSurface = false;
        _lastReboundAt = DateTimeOffset.MinValue;
        _recentRebounds.Clear();
    }

    private RecoveryDecision TryRebound(InstagramSurface surface, bool cancelFirst)
    {
        var now = _utcNow();
        if (now - _lastReboundAt < BounceCooldown)
        {
            // Still cooling down: cancel only if we haven't committed yet.
            return cancelFirst
                ? RecoveryDecision.CancelSilent(surface)
                : RecoveryDecision.Allow(surface);
        }

        while (_recentRebounds.Count > 0 && now - _recentRebounds.Peek() > LoopWindow)
        {
            _recentRebounds.Dequeue();
        }

        if (_recentRebounds.Count >= MaxReboundsPerWindow)
        {
            // Fail closed to the inbox rather than thrashing a poisoned URL.
            _lastValidDmUrl = NavigationPolicy.InboxUrl;
            _settledThreadKey = null;
        }

        _lastReboundAt = now;
        _recentRebounds.Enqueue(now);

        return cancelFirst
            ? RecoveryDecision.CancelAndRebound(surface, _lastValidDmUrl)
            : RecoveryDecision.ReboundTo(surface, _lastValidDmUrl);
    }

    private bool IsSameSettledThread(string? rawUrl)
    {
        if (_settledThreadKey is null)
        {
            return false;
        }

        if (!UrlCanonicalizer.TryCanonicalize(rawUrl, out var url))
        {
            return false;
        }

        return string.Equals(ThreadKey(url.Path), _settledThreadKey, StringComparison.Ordinal);
    }

    /// <summary>Coarse thread key: `/direct/t/{id}` → that path prefix;
    /// inbox/new → their path. Never includes query/fragment.</summary>
    public static string? ThreadKey(string path)
    {
        if (PathMatcher.Matches(path, "/direct/t"))
        {
            // `/direct/t/{id}/…` — keep `/direct/t/{id}`
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                return "/direct/t/" + parts[2];
            }
        }

        if (PathMatcher.Matches(path, "/direct/inbox"))
        {
            return "/direct/inbox";
        }

        if (PathMatcher.Matches(path, "/direct/new"))
        {
            return "/direct/new";
        }

        return null;
    }

    private static string ToAbsoluteUrl(CanonicalUrl url) =>
        "https://" + url.Host + url.Path;
}
