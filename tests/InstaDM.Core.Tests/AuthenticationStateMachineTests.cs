using InstaDM.Core.Authentication;
using Machine = InstaDM.Core.Authentication.AuthenticationStateMachine;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class AuthenticationStateMachineTests
{
    private static Machine MachineIn(AuthenticationState target)
    {
        var machine = new Machine();
        switch (target)
        {
            case AuthenticationState.Initializing:
                break;
            case AuthenticationState.Unauthenticated:
                machine.Handle(AuthenticationEvent.SessionCookieAbsent);
                break;
            case AuthenticationState.Authenticating:
                machine.Handle(AuthenticationEvent.AuthSurfaceCommitted);
                break;
            case AuthenticationState.ChallengeInProgress:
                machine.Handle(AuthenticationEvent.ChallengeSurfaceCommitted);
                break;
            case AuthenticationState.AuthenticatedPendingInbox:
                machine.Handle(AuthenticationEvent.SessionCookiePresent);
                break;
            case AuthenticationState.AuthenticatedInMessages:
                machine.Handle(AuthenticationEvent.SessionCookiePresent);
                machine.Handle(AuthenticationEvent.DirectSurfaceCommitted);
                break;
            case AuthenticationState.SessionExpired:
                machine.Handle(AuthenticationEvent.SessionCookiePresent);
                machine.Handle(AuthenticationEvent.DirectSurfaceCommitted);
                machine.Handle(AuthenticationEvent.SessionCookieAbsent);
                break;
            case AuthenticationState.Recovering:
                machine.Handle(AuthenticationEvent.WebProcessFailed);
                break;
        }
        Assert.AreEqual(target, machine.State, "test setup failed");
        return machine;
    }

    // ---------------- fresh start ----------------

    [TestMethod]
    public void FreshStart_NoCookie_BecomesUnauthenticatedAndWatches()
    {
        var machine = new Machine();
        var action = machine.Handle(AuthenticationEvent.SessionCookieAbsent);
        Assert.AreEqual(AuthenticationState.Unauthenticated, machine.State);
        Assert.AreEqual(Machine.Action.StartCookieWatch, action);
    }

    [TestMethod]
    public void FreshStart_ExistingSession_RoutesToInbox()
    {
        var machine = new Machine();
        var action = machine.Handle(AuthenticationEvent.SessionCookiePresent);
        Assert.AreEqual(AuthenticationState.AuthenticatedPendingInbox, machine.State);
        Assert.AreEqual(Machine.Action.NavigateToInbox, action);
    }

    // ---------------- login flows ----------------

    [TestMethod]
    public void FreshLogin_CookieDecides_NotSurfaceGuessing()
    {
        var machine = MachineIn(AuthenticationState.Unauthenticated);
        machine.Handle(AuthenticationEvent.AuthSurfaceCommitted);
        Assert.AreEqual(AuthenticationState.Authenticating, machine.State);

        // Cookie still absent while the user types: no state churn.
        Assert.AreEqual(Machine.Action.None, machine.Handle(AuthenticationEvent.SessionCookieAbsent));
        Assert.AreEqual(AuthenticationState.Authenticating, machine.State);

        // Delayed cookie appearance completes login.
        var action = machine.Handle(AuthenticationEvent.SessionCookiePresent);
        Assert.AreEqual(AuthenticationState.AuthenticatedPendingInbox, machine.State);
        Assert.AreEqual(Machine.Action.NavigateToInbox, action);

        // Inbox commits; watch stops.
        var settle = machine.Handle(AuthenticationEvent.DirectSurfaceCommitted);
        Assert.AreEqual(AuthenticationState.AuthenticatedInMessages, machine.State);
        Assert.AreEqual(Machine.Action.StopCookieWatch, settle);
    }

    [TestMethod]
    public void OneTapLogin_CookieWithoutAuthSurface_StillCompletes()
    {
        var machine = MachineIn(AuthenticationState.Unauthenticated);
        var action = machine.Handle(AuthenticationEvent.SessionCookiePresent);
        Assert.AreEqual(AuthenticationState.AuthenticatedPendingInbox, machine.State);
        Assert.AreEqual(Machine.Action.NavigateToInbox, action);
    }

    [TestMethod]
    public void ChallengeFlow_WaitsAndCompletesOnCookie()
    {
        var machine = MachineIn(AuthenticationState.Authenticating);
        machine.Handle(AuthenticationEvent.ChallengeSurfaceCommitted);
        Assert.AreEqual(AuthenticationState.ChallengeInProgress, machine.State);

        // Challenge takes a while; absent probes don't bounce the state.
        machine.Handle(AuthenticationEvent.SessionCookieAbsent);
        Assert.AreEqual(AuthenticationState.ChallengeInProgress, machine.State);

        var action = machine.Handle(AuthenticationEvent.SessionCookiePresent);
        Assert.AreEqual(AuthenticationState.AuthenticatedPendingInbox, machine.State);
        Assert.AreEqual(Machine.Action.NavigateToInbox, action);
    }

    [TestMethod]
    public void ChallengeAbandoned_BackToLogin()
    {
        var machine = MachineIn(AuthenticationState.ChallengeInProgress);
        machine.Handle(AuthenticationEvent.AuthSurfaceCommitted);
        Assert.AreEqual(AuthenticationState.Authenticating, machine.State);
    }

    [TestMethod]
    public void PostLoginCheckpoint_FromPendingInbox_HandledWithoutLoop()
    {
        var machine = MachineIn(AuthenticationState.AuthenticatedPendingInbox);
        var action = machine.Handle(AuthenticationEvent.ChallengeSurfaceCommitted);
        Assert.AreEqual(AuthenticationState.ChallengeInProgress, machine.State);
        Assert.AreEqual(Machine.Action.StartCookieWatch, action);
    }

    // ---------------- steady state, expiry, logout ----------------

    [TestMethod]
    public void SessionExpiry_InMessages_NavigatesToInboxForRelogin()
    {
        var machine = MachineIn(AuthenticationState.AuthenticatedInMessages);
        var action = machine.Handle(AuthenticationEvent.SessionCookieAbsent);
        Assert.AreEqual(AuthenticationState.SessionExpired, machine.State);
        Assert.AreEqual(Machine.Action.NavigateToInbox, action);

        // Instagram shows login; the machine follows.
        machine.Handle(AuthenticationEvent.AuthSurfaceCommitted);
        Assert.AreEqual(AuthenticationState.Authenticating, machine.State);
    }

    [TestMethod]
    public void ExplicitLogout_ThroughAuthSurface_RestartsWatch()
    {
        var machine = MachineIn(AuthenticationState.AuthenticatedInMessages);
        var action = machine.Handle(AuthenticationEvent.AuthSurfaceCommitted);
        Assert.AreEqual(AuthenticationState.Authenticating, machine.State);
        Assert.AreEqual(Machine.Action.StartCookieWatch, action);
    }

    [TestMethod]
    public void Relogin_AfterExpiry_Completes()
    {
        var machine = MachineIn(AuthenticationState.SessionExpired);
        machine.Handle(AuthenticationEvent.AuthSurfaceCommitted);
        machine.Handle(AuthenticationEvent.SessionCookiePresent);
        machine.Handle(AuthenticationEvent.DirectSurfaceCommitted);
        Assert.AreEqual(AuthenticationState.AuthenticatedInMessages, machine.State);
    }

    [TestMethod]
    public void ClearData_FromAnyState_LandsUnauthenticated()
    {
        foreach (var start in new[]
        {
            AuthenticationState.Unauthenticated,
            AuthenticationState.Authenticating,
            AuthenticationState.AuthenticatedInMessages,
            AuthenticationState.SessionExpired,
            AuthenticationState.Recovering,
        })
        {
            var machine = MachineIn(start);
            var action = machine.Handle(AuthenticationEvent.DataCleared);
            Assert.AreEqual(AuthenticationState.Unauthenticated, machine.State, start.ToString());
            Assert.AreEqual(Machine.Action.StartCookieWatch, action, start.ToString());
        }
    }

    [TestMethod]
    public void ClearData_FromFatal_ResetsAndRestartsWatch()
    {
        var machine = MachineIn(AuthenticationState.AuthenticatedInMessages);
        machine.Handle(AuthenticationEvent.WebProcessFailed);
        machine.Handle(AuthenticationEvent.WebProcessFailed);
        machine.Handle(AuthenticationEvent.WebProcessFailed);
        Assert.AreEqual(AuthenticationState.FatalWebRuntimeFailure, machine.State);

        var action = machine.Handle(AuthenticationEvent.DataCleared);
        Assert.AreEqual(AuthenticationState.Unauthenticated, machine.State);
        Assert.AreEqual(Machine.Action.StartCookieWatch, action);

        // Failure budget is fresh after clear-data.
        Assert.AreEqual(Machine.Action.NavigateToInbox,
            machine.Handle(AuthenticationEvent.WebProcessFailed));
        Assert.AreEqual(AuthenticationState.Recovering, machine.State);
    }

    // ---------------- duplicates and out-of-order events ----------------

    [TestMethod]
    public void DuplicateEvents_AreIdempotent_NoRepeatedActions()
    {
        var machine = MachineIn(AuthenticationState.Unauthenticated);
        Assert.AreEqual(Machine.Action.None, machine.Handle(AuthenticationEvent.SessionCookieAbsent));
        Assert.AreEqual(Machine.Action.None, machine.Handle(AuthenticationEvent.SessionCookieAbsent));

        machine.Handle(AuthenticationEvent.SessionCookiePresent);
        Assert.AreEqual(Machine.Action.None, machine.Handle(AuthenticationEvent.SessionCookiePresent));
        Assert.AreEqual(AuthenticationState.AuthenticatedPendingInbox, machine.State);
    }

    [TestMethod]
    public void DuplicateDirectCommits_InMessages_NoChurn()
    {
        var machine = MachineIn(AuthenticationState.AuthenticatedInMessages);
        Assert.AreEqual(Machine.Action.None, machine.Handle(AuthenticationEvent.DirectSurfaceCommitted));
        Assert.AreEqual(Machine.Action.None, machine.Handle(AuthenticationEvent.SessionCookiePresent));
        Assert.AreEqual(AuthenticationState.AuthenticatedInMessages, machine.State);
    }

    [TestMethod]
    public void DirectCommitBeforeCookieVerdict_StaysConservative()
    {
        var machine = new Machine(); // Initializing
        Assert.AreEqual(Machine.Action.None, machine.Handle(AuthenticationEvent.DirectSurfaceCommitted));
        Assert.AreEqual(AuthenticationState.Initializing, machine.State);
    }

    // ---------------- runtime failure ----------------

    [TestMethod]
    public void ProcessFailure_RecoversThenCookieProbeDecides()
    {
        var machine = MachineIn(AuthenticationState.AuthenticatedInMessages);
        var action = machine.Handle(AuthenticationEvent.WebProcessFailed);
        Assert.AreEqual(AuthenticationState.Recovering, machine.State);
        Assert.AreEqual(Machine.Action.NavigateToInbox, action);

        var recovered = machine.Handle(AuthenticationEvent.RecoveryNavigationSucceeded);
        Assert.AreEqual(AuthenticationState.Initializing, machine.State);
        Assert.AreEqual(Machine.Action.StartCookieWatch, recovered);

        machine.Handle(AuthenticationEvent.SessionCookiePresent);
        Assert.AreEqual(AuthenticationState.AuthenticatedPendingInbox, machine.State);
    }

    [TestMethod]
    public void RepeatedProcessFailure_DuringRecovery_RetriesThenGoesFatal()
    {
        var machine = MachineIn(AuthenticationState.AuthenticatedInMessages);

        Assert.AreEqual(Machine.Action.NavigateToInbox,
            machine.Handle(AuthenticationEvent.WebProcessFailed), "failure 1 retries");
        Assert.AreEqual(Machine.Action.NavigateToInbox,
            machine.Handle(AuthenticationEvent.WebProcessFailed), "failure 2 retries even in Recovering");
        Assert.AreEqual(Machine.Action.ShowFatalError,
            machine.Handle(AuthenticationEvent.WebProcessFailed), "failure 3 is fatal");
        Assert.AreEqual(AuthenticationState.FatalWebRuntimeFailure, machine.State);

        // Terminal: nothing moves it.
        Assert.AreEqual(Machine.Action.None, machine.Handle(AuthenticationEvent.SessionCookiePresent));
        Assert.AreEqual(Machine.Action.None, machine.Handle(AuthenticationEvent.WebProcessFailed));
        Assert.AreEqual(AuthenticationState.FatalWebRuntimeFailure, machine.State);
    }

    [TestMethod]
    public void SuccessfulRecovery_ResetsFailureBudget()
    {
        var machine = MachineIn(AuthenticationState.AuthenticatedInMessages);
        machine.Handle(AuthenticationEvent.WebProcessFailed);
        machine.Handle(AuthenticationEvent.WebProcessFailed);
        machine.Handle(AuthenticationEvent.RecoveryNavigationSucceeded);

        // Budget is fresh: the next failure retries instead of going fatal.
        var action = machine.Handle(AuthenticationEvent.WebProcessFailed);
        Assert.AreEqual(Machine.Action.NavigateToInbox, action);
        Assert.AreEqual(AuthenticationState.Recovering, machine.State);
    }
}
