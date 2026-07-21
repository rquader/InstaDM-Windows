namespace InstaDM.Core.Authentication;

/// <summary>
/// The explicit authentication lifecycle. The macOS source scattered this
/// across booleans in webview callbacks and grew defects in the gaps
/// (docs/SOURCE_BEHAVIOR.md B5/B6); here every transition is named, tested,
/// and driven by two privacy-safe signals only: session-cookie EXISTENCE
/// (never the value) and the committed surface category (never the URL).
/// </summary>
public enum AuthenticationState
{
    /// <summary>App start; session presence not yet determined.</summary>
    Initializing,

    /// <summary>No session cookie; Instagram is showing login.</summary>
    Unauthenticated,

    /// <summary>User is on a login/signup/recovery surface.</summary>
    Authenticating,

    /// <summary>User is inside a checkpoint/challenge/auth_platform flow.</summary>
    ChallengeInProgress,

    /// <summary>Session cookie exists; not yet settled on a DM surface.
    /// The watcher routes to the inbox from here.</summary>
    AuthenticatedPendingInbox,

    /// <summary>Session cookie exists and a DM surface is the main document.
    /// The steady state.</summary>
    AuthenticatedInMessages,

    /// <summary>A previously-present session cookie disappeared.</summary>
    SessionExpired,

    /// <summary>The web runtime failed; a recovery navigation is underway.</summary>
    Recovering,

    /// <summary>Repeated runtime failure; the app shows an honest error and
    /// stops retrying. Terminal until restart.</summary>
    FatalWebRuntimeFailure,
}

/// <summary>Everything that can move the authentication state machine.
/// Coarse and privacy-safe by construction — no URLs, no cookie values.</summary>
public enum AuthenticationEvent
{
    /// <summary>Cookie probe: session cookie exists.</summary>
    SessionCookiePresent,

    /// <summary>Cookie probe: session cookie absent.</summary>
    SessionCookieAbsent,

    /// <summary>An auth surface (login/signup/recovery/logout) committed.</summary>
    AuthSurfaceCommitted,

    /// <summary>A challenge / auth_platform surface committed.</summary>
    ChallengeSurfaceCommitted,

    /// <summary>A DM surface (inbox/thread/new) committed.</summary>
    DirectSurfaceCommitted,

    /// <summary>The web runtime's process failed.</summary>
    WebProcessFailed,

    /// <summary>A recovery navigation completed successfully.</summary>
    RecoveryNavigationSucceeded,

    /// <summary>The user ran "Clear Instagram data and sign out".</summary>
    DataCleared,
}
