namespace InstaDM.Core.Navigation;

/// <summary>
/// Feature gates the navigation policy consults. Immutable — construct a new
/// policy when settings change. Compile-time availability and runtime opt-in
/// are resolved by the feature modules (see <c>Features/</c>) before this is
/// built, so the policy itself stays pure.
/// </summary>
public sealed record PolicyOptions
{
    /// <summary>Opt-in Follow Requests surface (`/accounts/activity`). Default off.</summary>
    public bool FollowRequestsEnabled { get; init; }

    /// <summary>Opt-in shared posts/reels from DMs (`/p`, `/reel`, `/reels`, `/tv`),
    /// additionally source-gated to clicks originating in `/direct/*`.
    /// Default off; compile-time unavailable in the first release
    /// (docs/SOURCE_BEHAVIOR.md B11).</summary>
    public bool SharedPostsEnabled { get; init; }

    public static readonly PolicyOptions Default = new();
}
