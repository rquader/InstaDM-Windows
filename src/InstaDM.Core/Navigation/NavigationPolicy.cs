namespace InstaDM.Core.Navigation;

/// <summary>
/// The canonical Instagram surface policy — the single source of truth for
/// what the embedded web view may request, what may become the visible page,
/// and what the document-start JavaScript guard receives (via
/// <see cref="PolicyScriptBuilder"/>).
///
/// Layered exactly like the proven macOS design (docs/SOURCE_BEHAVIOR.md B2):
/// <list type="number">
/// <item><see cref="DecideNetworkRequest"/> — may this URL be requested at
/// all, including XHR/fetch subresources.</item>
/// <item><see cref="IsUserSurface"/> — may this path persist as the visible
/// main document after login (internal endpoints may load but not persist).</item>
/// <item><see cref="IsDirectMessagingPath"/> — the narrow DM routes only.
/// Bare `/direct` is the minimized-messenger shell that renders full
/// Instagram and is deliberately NOT a DM path.</item>
/// </list>
///
/// Everything here is pure and deterministic; feature gates come in through
/// <see cref="PolicyOptions"/>, source context through
/// <see cref="NavigationContext"/>.
/// </summary>
public sealed class NavigationPolicy
{
    /// <summary>URL the Messages surface loads on launch and recovery.</summary>
    public const string InboxUrl = "https://www.instagram.com/direct/inbox/";

    /// <summary>URL of the opt-in follow-requests surface.</summary>
    public const string FollowRequestsUrl = "https://www.instagram.com/accounts/activity/?followRequests=1";

    internal static readonly IReadOnlyList<string> AllowedHosts =
    [
        "www.instagram.com",
        "instagram.com",
        "accounts.instagram.com",
    ];

    /// <summary>The dedicated auth host — allowed on host alone.</summary>
    internal const string AuthOnlyHost = "accounts.instagram.com";

    /// <summary>Narrow auth subpaths — deliberately NOT a blanket `/accounts`
    /// (which leaked notifications/activity/edit surfaces in the source app).</summary>
    internal static readonly IReadOnlyList<string> AuthAccountPathPrefixes =
    [
        "/accounts/login",
        "/accounts/onetap",
        "/accounts/password",
        "/accounts/signup",
        "/accounts/emailsignup",
        "/accounts/check_email",
        "/accounts/logout",
        "/accounts/confirm",
        "/accounts/access",
        "/accounts/account_recovery",
        "/accounts/username",
        "/accounts/two_factor",
    ];

    /// <summary>All auth stand-down surfaces: account auth paths, security
    /// checkpoints, and `/auth_platform` (Instagram's login-verification /
    /// bot-check flow — omitting it hung fresh login forever in the source
    /// app; see docs/SOURCE_BEHAVIOR.md B5).</summary>
    public static readonly IReadOnlyList<string> AuthSurfacePathPrefixes =
        [.. AuthAccountPathPrefixes, "/challenge", "/auth_platform"];

    /// <summary>Page-internal endpoints — allowed as subresources, never as
    /// the visible main document.</summary>
    internal static readonly IReadOnlyList<string> InternalEndpointPrefixes =
    [
        "/api",
        "/graphql",
        "/ajax",
        "/static",
    ];

    /// <summary>The DM surfaces the app exposes. NOT bare `/direct`.</summary>
    public static readonly IReadOnlyList<string> DirectMessagingPathPrefixes =
    [
        "/direct/inbox",
        "/direct/t",
        "/direct/new",
    ];

    /// <summary>Prefixes granted by the opt-in FollowRequests feature.</summary>
    public static readonly IReadOnlyList<string> FollowRequestsPathPrefixes =
    [
        "/accounts/activity",
    ];

    /// <summary>Prefixes granted by the opt-in SharedPosts feature (source-gated).</summary>
    public static readonly IReadOnlyList<string> SharedPostsPathPrefixes =
    [
        "/p",
        "/reel",
        "/reels",
        "/tv",
    ];

    private static readonly HashSet<string> ReservedTopLevelSegments = new(StringComparer.Ordinal)
    {
        "direct", "accounts", "explore", "reels", "reel", "stories", "p", "tv",
        "about", "legal", "api", "graphql", "ajax", "static", "challenge",
        "auth_platform", "directory", "session", "nametag", "web", "developer",
        "privacy", "terms", "lite", "create", "your_activity", "saved",
    };

    private readonly PolicyOptions _options;

    public NavigationPolicy(PolicyOptions? options = null)
    {
        _options = options ?? PolicyOptions.Default;
    }

    public PolicyOptions Options => _options;

    // ------------------------------------------------------------------
    // Classification
    // ------------------------------------------------------------------

    /// <summary>Classifies a raw URL into the surface taxonomy. Fails closed:
    /// anything that cannot be canonicalized is <see cref="InstagramSurface.Malformed"/>.</summary>
    public InstagramSurface Classify(string? rawUrl)
    {
        if (!UrlCanonicalizer.TryCanonicalize(rawUrl, out var url))
        {
            return InstagramSurface.Malformed;
        }

        return Classify(url);
    }

    public InstagramSurface Classify(CanonicalUrl url)
    {
        if (!AllowedHosts.Contains(url.Host))
        {
            return InstagramSurface.OffPlatform;
        }

        if (url.Host == AuthOnlyHost)
        {
            return InstagramSurface.AuthHost;
        }

        var path = url.Path;

        if (path == "/")
        {
            return InstagramSurface.HomeFeed;
        }

        if (PathMatcher.Matches(path, "/direct/inbox")) { return InstagramSurface.DirectInbox; }
        if (PathMatcher.Matches(path, "/direct/t")) { return InstagramSurface.DirectThread; }
        if (PathMatcher.Matches(path, "/direct/new")) { return InstagramSurface.DirectNew; }
        if (PathMatcher.Matches(path, "/direct")) { return InstagramSurface.DirectShell; }

        // FollowRequests before the generic auth check: /accounts/activity is
        // an opt-in product surface, never an auth page.
        if (PathMatcher.MatchesAny(path, FollowRequestsPathPrefixes)) { return InstagramSurface.FollowRequests; }
        if (PathMatcher.MatchesAny(path, AuthAccountPathPrefixes)) { return InstagramSurface.AuthAccount; }
        if (PathMatcher.Matches(path, "/challenge")) { return InstagramSurface.AuthChallenge; }
        if (PathMatcher.Matches(path, "/auth_platform")) { return InstagramSurface.AuthPlatform; }
        if (PathMatcher.MatchesAny(path, InternalEndpointPrefixes)) { return InstagramSurface.InternalEndpoint; }
        if (PathMatcher.Matches(path, "/explore")) { return InstagramSurface.Explore; }
        if (PathMatcher.Matches(path, "/reel") || PathMatcher.Matches(path, "/reels")) { return InstagramSurface.Reels; }
        if (PathMatcher.Matches(path, "/stories")) { return InstagramSurface.Stories; }
        if (PathMatcher.Matches(path, "/p") || PathMatcher.Matches(path, "/tv")) { return InstagramSurface.Post; }

        if (IsProfilePath(path)) { return InstagramSurface.Profile; }

        return InstagramSurface.UnknownInstagram;
    }

    // ------------------------------------------------------------------
    // Layer 1 — network requests
    // ------------------------------------------------------------------

    /// <summary>May this URL be requested at all (top-level or subresource)?</summary>
    public NavigationDecision DecideNetworkRequest(string? rawUrl, NavigationContext? context = null)
    {
        context ??= NavigationContext.None;
        var surface = Classify(rawUrl);

        return surface switch
        {
            InstagramSurface.Malformed => NavigationDecision.Block(DecisionReason.BlockedMalformed, surface),
            InstagramSurface.OffPlatform => NavigationDecision.Block(DecisionReason.BlockedOffPlatform, surface),
            InstagramSurface.AuthHost => NavigationDecision.Allow(DecisionReason.AllowedAuthHost, surface),
            InstagramSurface.DirectInbox or InstagramSurface.DirectThread or InstagramSurface.DirectNew =>
                NavigationDecision.Allow(DecisionReason.AllowedDmSurface, surface),
            InstagramSurface.DirectShell => NavigationDecision.Block(DecisionReason.BlockedDirectShell, surface),
            InstagramSurface.AuthAccount or InstagramSurface.AuthChallenge or InstagramSurface.AuthPlatform =>
                NavigationDecision.Allow(DecisionReason.AllowedAuthSurface, surface),
            InstagramSurface.InternalEndpoint =>
                NavigationDecision.Allow(DecisionReason.AllowedInternalEndpoint, surface),
            InstagramSurface.HomeFeed => NavigationDecision.Block(DecisionReason.BlockedHomeFeed, surface),
            InstagramSurface.Explore => NavigationDecision.Block(DecisionReason.BlockedExplore, surface),
            InstagramSurface.Stories => NavigationDecision.Block(DecisionReason.BlockedStories, surface),
            InstagramSurface.Profile => NavigationDecision.Block(DecisionReason.BlockedProfile, surface),
            InstagramSurface.FollowRequests => _options.FollowRequestsEnabled
                ? NavigationDecision.Allow(DecisionReason.AllowedFollowRequests, surface)
                : NavigationDecision.Block(DecisionReason.BlockedFollowRequestsDisabled, surface),
            InstagramSurface.Post or InstagramSurface.Reels => DecideSharedPost(surface, context),
            _ => NavigationDecision.Block(DecisionReason.BlockedUnknownSurface, surface),
        };
    }

    private NavigationDecision DecideSharedPost(InstagramSurface surface, NavigationContext context)
    {
        if (!_options.SharedPostsEnabled)
        {
            return NavigationDecision.Block(DecisionReason.BlockedSharedPostsDisabled, surface);
        }

        return context.FromDirect
            ? NavigationDecision.Allow(DecisionReason.AllowedSharedPostFromDm, surface)
            : NavigationDecision.Block(DecisionReason.BlockedSharedPostNotFromDm, surface);
    }

    // ------------------------------------------------------------------
    // Layer 2 — main-document gate
    // ------------------------------------------------------------------

    /// <summary>May this URL persist as the visible main document? Internal
    /// endpoints are request-allowed but must never become the page.</summary>
    public bool IsUserSurface(string? rawUrl, NavigationContext? context = null)
    {
        context ??= NavigationContext.None;
        var surface = Classify(rawUrl);

        return surface switch
        {
            InstagramSurface.DirectInbox or InstagramSurface.DirectThread or InstagramSurface.DirectNew => true,
            InstagramSurface.AuthAccount or InstagramSurface.AuthChallenge or InstagramSurface.AuthPlatform
                or InstagramSurface.AuthHost => true,
            InstagramSurface.FollowRequests => _options.FollowRequestsEnabled,
            InstagramSurface.Post or InstagramSurface.Reels =>
                _options.SharedPostsEnabled && context.FromDirect,
            _ => false,
        };
    }

    // ------------------------------------------------------------------
    // Layer 3 — helpers used by the coordinator and recovery logic
    // ------------------------------------------------------------------

    /// <summary>True only for the narrow DM routes (`/direct/inbox`,
    /// `/direct/t`, `/direct/new`). Bare `/direct` deliberately fails.</summary>
    public static bool IsDirectMessagingPath(string path) =>
        PathMatcher.MatchesAny(path, DirectMessagingPathPrefixes);

    /// <summary>True when the path is a login / recovery / challenge /
    /// auth_platform surface where containment must stand down.</summary>
    public static bool IsAuthSurfacePath(string path) =>
        PathMatcher.MatchesAny(path, AuthSurfacePathPrefixes);

    /// <summary>`/{username}/` — the classic one-click profile escape.</summary>
    public static bool IsProfilePath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 1)
        {
            return false;
        }

        var segment = segments[0];
        if (ReservedTopLevelSegments.Contains(segment.ToLowerInvariant()))
        {
            return false;
        }

        foreach (var c in segment)
        {
            var ok = char.IsAsciiLetterOrDigit(c) || c is '.' or '_';
            if (!ok)
            {
                return false;
            }
        }

        return segment.Length > 0;
    }

    /// <summary>Background hops Instagram fires while the user sits on DMs
    /// (feed prefetch, explore, account-linking, notification chrome).
    /// These are cancelled silently — no stopLoading, no recovery — because
    /// reacting to them caused reload/scroll-snap thrash in the source app
    /// (docs/SOURCE_BEHAVIOR.md B7).</summary>
    public bool IsIncidentalBlockedPrefetch(string? rawUrl)
    {
        if (!UrlCanonicalizer.TryCanonicalize(rawUrl, out var url))
        {
            // Off-platform / malformed background noise: drop silently.
            return true;
        }

        if (!AllowedHosts.Contains(url.Host))
        {
            return true;
        }

        var path = url.Path;
        if (path == "/") { return true; }
        if (PathMatcher.Matches(path, "/explore")) { return true; }
        if (PathMatcher.Matches(path, "/reels") || PathMatcher.Matches(path, "/reel")) { return true; }
        if (path.Contains("notifications", StringComparison.Ordinal)) { return true; }
        if (PathMatcher.Matches(path, "/accounts/manage")) { return true; }
        if (PathMatcher.Matches(path, "/accounts/link")) { return true; }
        if (PathMatcher.Matches(path, "/accounts/connected")) { return true; }
        // Profile *prefetches* are incidental too — the JS guard blocks real
        // profile clicks; reacting to hover/viewport prefetches here caused
        // the heal-thrash defect (macOS audit H1). Explicit link activations
        // are handled by the coordinator, not this predicate.
        if (IsProfilePath(path)) { return true; }
        return false;
    }

    /// <summary>Main document committed outside DMs (feed, profile, the bare
    /// `/direct` shell, stories) — recovery should bounce back.</summary>
    public bool ShouldRecoverFromMainDocument(string? rawUrl, NavigationContext? context = null)
    {
        context ??= NavigationContext.None;
        var surface = Classify(rawUrl);

        return surface switch
        {
            InstagramSurface.Malformed or InstagramSurface.OffPlatform => true,
            InstagramSurface.HomeFeed or InstagramSurface.DirectShell
                or InstagramSurface.Profile or InstagramSurface.Explore
                or InstagramSurface.Reels or InstagramSurface.Stories
                or InstagramSurface.Post => !IsUserSurface(rawUrl, context),
            _ => !IsUserSurface(rawUrl, context) && !DecideNetworkRequest(rawUrl, context).IsAllowed,
        };
    }
}
