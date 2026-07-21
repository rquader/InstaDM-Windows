namespace InstaDM.Core.Navigation;

/// <summary>Why a navigation was allowed or blocked. Coarse, privacy-safe
/// categories — these are the ONLY things that may appear in diagnostics;
/// never raw URLs (see docs/PRIVACY_THREAT_MODEL.md T2/T3).</summary>
public enum DecisionReason
{
    // Allow reasons
    AllowedDmSurface,
    AllowedAuthSurface,
    AllowedAuthHost,
    AllowedInternalEndpoint,
    AllowedFollowRequests,
    AllowedSharedPostFromDm,

    // Block reasons
    BlockedMalformed,
    BlockedOffPlatform,
    BlockedHomeFeed,
    BlockedDirectShell,
    BlockedProfile,
    BlockedExplore,
    BlockedReels,
    BlockedStories,
    BlockedPost,
    BlockedNonAuthAccounts,
    BlockedFollowRequestsDisabled,
    BlockedSharedPostsDisabled,
    BlockedSharedPostNotFromDm,
    BlockedUnknownSurface,
}

/// <summary>
/// The outcome of a policy question, carrying the classified surface and an
/// explicit reason. Immutable and comparable so table-driven tests can
/// assert on the full decision, not just a boolean.
/// </summary>
public sealed record NavigationDecision(bool IsAllowed, DecisionReason Reason, InstagramSurface Surface)
{
    public static NavigationDecision Allow(DecisionReason reason, InstagramSurface surface) =>
        new(true, reason, surface);

    public static NavigationDecision Block(DecisionReason reason, InstagramSurface surface) =>
        new(false, reason, surface);
}

/// <summary>
/// Context a navigation carries into the policy. <see cref="None"/> (unknown
/// origin) is the strictest interpretation — source-gated opt-in surfaces
/// are not granted.
/// </summary>
public sealed record NavigationContext(bool FromDirect)
{
    public static readonly NavigationContext None = new(FromDirect: false);
    public static readonly NavigationContext FromDm = new(FromDirect: true);
}
