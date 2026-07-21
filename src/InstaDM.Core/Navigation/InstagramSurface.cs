namespace InstaDM.Core.Navigation;

/// <summary>
/// The canonical taxonomy of Instagram surfaces the app reasons about.
/// Every navigation decision names one of these so logs and tests speak in
/// coarse, privacy-safe categories instead of raw URLs.
/// </summary>
public enum InstagramSurface
{
    /// <summary>Unparseable, non-https, or otherwise malformed input. Always blocked (fail closed).</summary>
    Malformed,

    /// <summary>A host outside the Instagram allowlist (including lookalikes). Blocked.</summary>
    OffPlatform,

    /// <summary>The DM inbox (`/direct/inbox`).</summary>
    DirectInbox,

    /// <summary>A DM thread (`/direct/t/...`), one-to-one or group.</summary>
    DirectThread,

    /// <summary>The new-message composer (`/direct/new`).</summary>
    DirectNew,

    /// <summary>Bare `/direct` — the minimized-messenger shell that renders full Instagram. Blocked.</summary>
    DirectShell,

    /// <summary>Login, one-tap, recovery, signup, logout, and other narrow `/accounts/*` auth subpaths.</summary>
    AuthAccount,

    /// <summary>Security checkpoint (`/challenge/*`).</summary>
    AuthChallenge,

    /// <summary>Instagram's login-verification / bot-check flow (`/auth_platform/*`).
    /// Missing this from the macOS allowlist silently broke fresh login (spinner hang).</summary>
    AuthPlatform,

    /// <summary>The dedicated auth host (`accounts.instagram.com`), allowed on host alone.</summary>
    AuthHost,

    /// <summary>Page-internal endpoints (`/api`, `/graphql`, `/ajax`, `/static`) — allowed as
    /// subresources, never as the visible main document.</summary>
    InternalEndpoint,

    /// <summary>The home feed (`/`). Blocked; also the post-login redirect hop.</summary>
    HomeFeed,

    /// <summary>Explore (`/explore/*`). Blocked.</summary>
    Explore,

    /// <summary>Reels (`/reel`, `/reels`). Blocked unless SharedPosts gating applies.</summary>
    Reels,

    /// <summary>Stories (`/stories/*`). Blocked.</summary>
    Stories,

    /// <summary>A single post (`/p/*`) or IGTV (`/tv/*`). Blocked unless SharedPosts gating applies.</summary>
    Post,

    /// <summary>A profile page (`/{username}/`). Blocked.</summary>
    Profile,

    /// <summary>The opt-in follow-requests surface (`/accounts/activity`).</summary>
    FollowRequests,

    /// <summary>Anything else on an allowed Instagram host that no rule recognizes. Blocked.</summary>
    UnknownInstagram,
}
