namespace InstaDM.Core.Authentication;

/// <summary>
/// Pure, deterministic authentication state machine. Feed it events; it
/// returns the new state and the single action the host should take. It
/// holds no timers, no webview, no cookie access — those live in adapters —
/// so every path is unit-testable on any platform.
///
/// Design rules distilled from the macOS lessons:
/// <list type="bullet">
/// <item>Login completion is decided by cookie EXISTENCE, never by guessing
/// redirects — Instagram's login flows (fresh, one-tap, 2FA, checkpoint,
/// auth_platform bot-check) hop through unpredictable routes.</item>
/// <item>Duplicate and out-of-order events must be harmless: every
/// transition is idempotent (same event in same state = no change, no
/// action) and unknown combinations stay put rather than guessing.</item>
/// <item>A challenge is not an error; the machine waits it out and only the
/// cookie decides the outcome.</item>
/// <item>Process failure is capped: repeated failures reach
/// <see cref="AuthenticationState.FatalWebRuntimeFailure"/> instead of a
/// reload loop.</item>
/// </list>
/// </summary>
public sealed class AuthenticationStateMachine
{
    /// <summary>Consecutive web-process failures tolerated before fatal.</summary>
    public const int MaxProcessFailures = 3;

    private int _processFailures;

    public AuthenticationState State { get; private set; } = AuthenticationState.Initializing;

    /// <summary>What the host should do after a transition.</summary>
    public enum Action
    {
        /// <summary>Nothing to do.</summary>
        None,

        /// <summary>Navigate the webview to the DM inbox.</summary>
        NavigateToInbox,

        /// <summary>Start (or keep) the bounded cookie-presence poller.</summary>
        StartCookieWatch,

        /// <summary>Stop the cookie-presence poller.</summary>
        StopCookieWatch,

        /// <summary>Show the honest fatal-runtime error UI.</summary>
        ShowFatalError,
    }

    public Action Handle(AuthenticationEvent evt)
    {
        // Clear-data is the one escape hatch from every state, including fatal:
        // the user explicitly wiped the profile and expects a clean login.
        if (evt == AuthenticationEvent.DataCleared)
        {
            _processFailures = 0;
            if (State == AuthenticationState.Unauthenticated)
            {
                return Action.StartCookieWatch; // may already be watching; Start is idempotent
            }

            State = AuthenticationState.Unauthenticated;
            return Action.StartCookieWatch;
        }

        if (State == AuthenticationState.FatalWebRuntimeFailure)
        {
            return Action.None; // terminal until restart or clear-data
        }

        // Process failure bypasses the idempotency shortcut: a failure while
        // already Recovering means the recovery navigation itself died and
        // must be retried (or capped) — swallowing it would strand the app.
        if (evt == AuthenticationEvent.WebProcessFailed)
        {
            _processFailures++;
            if (_processFailures >= MaxProcessFailures)
            {
                State = AuthenticationState.FatalWebRuntimeFailure;
                return Action.ShowFatalError;
            }

            State = AuthenticationState.Recovering;
            return Action.NavigateToInbox;
        }

        if (evt == AuthenticationEvent.RecoveryNavigationSucceeded)
        {
            _processFailures = 0;
        }

        var (next, action) = Decide(evt);
        if (next == State)
        {
            // Idempotent: re-delivered events cause no repeated actions.
            return Action.None;
        }

        State = next;
        return action;
    }

    private (AuthenticationState next, Action action) Decide(AuthenticationEvent evt)
    {
        return State switch
        {
            AuthenticationState.Initializing => evt switch
            {
                AuthenticationEvent.SessionCookiePresent =>
                    (AuthenticationState.AuthenticatedPendingInbox, Action.NavigateToInbox),
                AuthenticationEvent.SessionCookieAbsent =>
                    (AuthenticationState.Unauthenticated, Action.StartCookieWatch),
                AuthenticationEvent.AuthSurfaceCommitted =>
                    (AuthenticationState.Authenticating, Action.StartCookieWatch),
                AuthenticationEvent.ChallengeSurfaceCommitted =>
                    (AuthenticationState.ChallengeInProgress, Action.StartCookieWatch),
                AuthenticationEvent.DirectSurfaceCommitted =>
                    // Surface commit without a cookie verdict yet: stay
                    // conservative, wait for the probe.
                    (State, Action.None),
                _ => (State, Action.None),
            },

            AuthenticationState.Unauthenticated => evt switch
            {
                AuthenticationEvent.SessionCookiePresent =>
                    // One-tap relogin can complete without ever committing a
                    // recognizable auth surface — the cookie alone decides.
                    (AuthenticationState.AuthenticatedPendingInbox, Action.NavigateToInbox),
                AuthenticationEvent.AuthSurfaceCommitted =>
                    (AuthenticationState.Authenticating, Action.None),
                AuthenticationEvent.ChallengeSurfaceCommitted =>
                    (AuthenticationState.ChallengeInProgress, Action.None),
                _ => (State, Action.None),
            },

            AuthenticationState.Authenticating => evt switch
            {
                AuthenticationEvent.SessionCookiePresent =>
                    (AuthenticationState.AuthenticatedPendingInbox, Action.NavigateToInbox),
                AuthenticationEvent.ChallengeSurfaceCommitted =>
                    (AuthenticationState.ChallengeInProgress, Action.None),
                AuthenticationEvent.SessionCookieAbsent =>
                    // Still on login; not a regression. Stay.
                    (State, Action.None),
                _ => (State, Action.None),
            },

            AuthenticationState.ChallengeInProgress => evt switch
            {
                AuthenticationEvent.SessionCookiePresent =>
                    (AuthenticationState.AuthenticatedPendingInbox, Action.NavigateToInbox),
                AuthenticationEvent.AuthSurfaceCommitted =>
                    // Challenge failed/abandoned back to login.
                    (AuthenticationState.Authenticating, Action.None),
                _ => (State, Action.None),
            },

            AuthenticationState.AuthenticatedPendingInbox => evt switch
            {
                AuthenticationEvent.DirectSurfaceCommitted =>
                    (AuthenticationState.AuthenticatedInMessages, Action.StopCookieWatch),
                AuthenticationEvent.SessionCookieAbsent =>
                    (AuthenticationState.SessionExpired, Action.NavigateToInbox),
                AuthenticationEvent.ChallengeSurfaceCommitted =>
                    // Post-login checkpoint (new device verification).
                    (AuthenticationState.ChallengeInProgress, Action.StartCookieWatch),
                _ => (State, Action.None),
            },

            AuthenticationState.AuthenticatedInMessages => evt switch
            {
                AuthenticationEvent.SessionCookieAbsent =>
                    // Expiry or logout; Instagram will show login at inbox.
                    (AuthenticationState.SessionExpired, Action.NavigateToInbox),
                AuthenticationEvent.AuthSurfaceCommitted =>
                    // Explicit logout navigates through /accounts/logout.
                    (AuthenticationState.Authenticating, Action.StartCookieWatch),
                AuthenticationEvent.ChallengeSurfaceCommitted =>
                    (AuthenticationState.ChallengeInProgress, Action.StartCookieWatch),
                _ => (State, Action.None),
            },

            AuthenticationState.SessionExpired => evt switch
            {
                AuthenticationEvent.AuthSurfaceCommitted =>
                    (AuthenticationState.Authenticating, Action.StartCookieWatch),
                AuthenticationEvent.SessionCookiePresent =>
                    (AuthenticationState.AuthenticatedPendingInbox, Action.NavigateToInbox),
                AuthenticationEvent.SessionCookieAbsent =>
                    (AuthenticationState.Unauthenticated, Action.StartCookieWatch),
                _ => (State, Action.None),
            },

            AuthenticationState.Recovering => evt switch
            {
                AuthenticationEvent.RecoveryNavigationSucceeded =>
                    // Cookie probe decides where we actually are next.
                    (AuthenticationState.Initializing, Action.StartCookieWatch),
                _ => (State, Action.None),
            },

            _ => (State, Action.None),
        };
    }
}
