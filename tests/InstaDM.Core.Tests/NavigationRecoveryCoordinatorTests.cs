using InstaDM.Core.Navigation;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class NavigationRecoveryCoordinatorTests
{
    private sealed class Clock
    {
        public DateTimeOffset Now { get; set; } = new(2026, 7, 22, 0, 0, 0, TimeSpan.Zero);
        public void Advance(TimeSpan delta) => Now += delta;
    }

    private static (NavigationRecoveryCoordinator coordinator, Clock clock) Create()
    {
        var clock = new Clock();
        return (new NavigationRecoveryCoordinator(utcNow: () => clock.Now), clock);
    }

    [TestMethod]
    public void AllowedDmNavigation_PassesThrough()
    {
        var (c, _) = Create();
        var decision = c.OnNavigationStarting(
            "https://www.instagram.com/direct/inbox/",
            NavigationInitiator.UserActivated);
        Assert.AreEqual(RecoveryActionKind.None, decision.Action);
    }

    [TestMethod]
    public void IncidentalFeedPrefetch_CancelsSilently_NoRebound()
    {
        var (c, _) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/inbox/");

        var decision = c.OnNavigationStarting(
            "https://www.instagram.com/",
            NavigationInitiator.Other);
        Assert.AreEqual(RecoveryActionKind.CancelSilent, decision.Action);
        Assert.IsNull(decision.ReboundUrl);
        Assert.AreEqual(InstagramSurface.HomeFeed, decision.Surface);
    }

    [TestMethod]
    public void ProfilePrefetch_CancelsSilently_EvenIfUserFlagUnset()
    {
        var (c, _) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/t/ABC/");

        var decision = c.OnNavigationStarting(
            "https://www.instagram.com/some_user/",
            NavigationInitiator.Other);
        Assert.AreEqual(RecoveryActionKind.CancelSilent, decision.Action);
        Assert.IsNull(decision.ReboundUrl);
    }

    [TestMethod]
    public void UserActivatedEscape_CancelsAndReboundsToLastThread()
    {
        var (c, _) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/t/THREAD1/");

        var decision = c.OnNavigationStarting(
            "https://www.instagram.com/explore/",
            NavigationInitiator.UserActivated);
        Assert.AreEqual(RecoveryActionKind.CancelAndRebound, decision.Action);
        Assert.AreEqual("https://www.instagram.com/direct/t/THREAD1/", decision.ReboundUrl);
        Assert.AreEqual(InstagramSurface.Explore, decision.Surface);
    }

    [TestMethod]
    public void CommittedOffPolicyDocument_ReboundsToLastValid()
    {
        var (c, _) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/inbox/");

        var decision = c.OnMainDocumentCommitted("https://www.instagram.com/");
        Assert.AreEqual(RecoveryActionKind.Rebound, decision.Action);
        Assert.AreEqual(NavigationPolicy.InboxUrl, decision.ReboundUrl);
    }

    [TestMethod]
    public void SameThreadRenavigation_AfterSettle_IsSuppressed()
    {
        var (c, _) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/t/ABC/");
        Assert.IsTrue(c.HasSettledOnUserSurface);

        var decision = c.OnNavigationStarting(
            "https://www.instagram.com/direct/t/ABC/",
            NavigationInitiator.Other);
        Assert.AreEqual(RecoveryActionKind.CancelSameThread, decision.Action);
    }

    [TestMethod]
    public void SameThreadRenavigation_BeforeSettle_IsAllowed()
    {
        var (c, _) = Create();
        // Cold launch: never settled. Same URL must not be suppressed or
        // the first inbox load white-screens.
        var decision = c.OnNavigationStarting(
            "https://www.instagram.com/direct/inbox/",
            NavigationInitiator.Other);
        Assert.AreEqual(RecoveryActionKind.None, decision.Action);
        Assert.IsFalse(c.HasSettledOnUserSurface);
    }

    [TestMethod]
    public void UserSwitchingThreads_IsAllowed()
    {
        var (c, _) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/t/ABC/");

        var decision = c.OnNavigationStarting(
            "https://www.instagram.com/direct/t/XYZ/",
            NavigationInitiator.UserActivated);
        Assert.AreEqual(RecoveryActionKind.None, decision.Action);
    }

    [TestMethod]
    public void BounceCooldown_SuppressesImmediateSecondRebound()
    {
        var (c, clock) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/inbox/");

        var first = c.OnNavigationStarting(
            "https://www.instagram.com/explore/",
            NavigationInitiator.UserActivated);
        Assert.AreEqual(RecoveryActionKind.CancelAndRebound, first.Action);

        var second = c.OnNavigationStarting(
            "https://www.instagram.com/reels/",
            NavigationInitiator.UserActivated);
        Assert.AreEqual(RecoveryActionKind.CancelSilent, second.Action);

        clock.Advance(NavigationRecoveryCoordinator.BounceCooldown);
        var third = c.OnNavigationStarting(
            "https://www.instagram.com/reels/",
            NavigationInitiator.UserActivated);
        Assert.AreEqual(RecoveryActionKind.CancelAndRebound, third.Action);
    }

    [TestMethod]
    public void ReboundLoopCap_FailsClosedToInbox()
    {
        var (c, clock) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/t/THREAD1/");

        for (var i = 0; i < NavigationRecoveryCoordinator.MaxReboundsPerWindow; i++)
        {
            var d = c.OnNavigationStarting(
                "https://www.instagram.com/explore/",
                NavigationInitiator.UserActivated);
            Assert.AreEqual(RecoveryActionKind.CancelAndRebound, d.Action);
            Assert.AreEqual("https://www.instagram.com/direct/t/THREAD1/", d.ReboundUrl);
            clock.Advance(NavigationRecoveryCoordinator.BounceCooldown);
        }

        var capped = c.OnNavigationStarting(
            "https://www.instagram.com/explore/",
            NavigationInitiator.UserActivated);
        Assert.AreEqual(RecoveryActionKind.CancelAndRebound, capped.Action);
        Assert.AreEqual(NavigationPolicy.InboxUrl, capped.ReboundUrl);
        Assert.AreEqual(NavigationPolicy.InboxUrl, c.LastValidDmUrl);
    }

    [TestMethod]
    public void GuardBlocked_DoesNotRebound()
    {
        var (c, _) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/t/ABC/");
        var decision = c.OnGuardBlocked(InstagramSurface.Profile);
        Assert.AreEqual(RecoveryActionKind.CancelSilent, decision.Action);
        Assert.AreEqual("https://www.instagram.com/direct/t/ABC/", c.LastValidDmUrl);
    }

    [TestMethod]
    public void Reset_ClearsSettledStateAndLastValid()
    {
        var (c, _) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/t/ABC/");
        c.Reset();
        Assert.IsFalse(c.HasSettledOnUserSurface);
        Assert.AreEqual(NavigationPolicy.InboxUrl, c.LastValidDmUrl);
    }

    [TestMethod]
    public void AuthSurfaceCommit_SettlesWithoutChangingLastDm()
    {
        var (c, _) = Create();
        c.OnMainDocumentCommitted("https://www.instagram.com/direct/inbox/");
        var before = c.LastValidDmUrl;

        var decision = c.OnMainDocumentCommitted("https://www.instagram.com/accounts/login/");
        Assert.AreEqual(RecoveryActionKind.None, decision.Action);
        Assert.AreEqual(before, c.LastValidDmUrl);
        Assert.IsTrue(c.HasSettledOnUserSurface);
    }

    [TestMethod]
    public void ThreadKey_ExtractsStableId()
    {
        Assert.AreEqual("/direct/t/ABC", NavigationRecoveryCoordinator.ThreadKey("/direct/t/ABC/"));
        Assert.AreEqual("/direct/t/ABC", NavigationRecoveryCoordinator.ThreadKey("/direct/t/ABC/details/"));
        Assert.AreEqual("/direct/inbox", NavigationRecoveryCoordinator.ThreadKey("/direct/inbox/"));
        Assert.IsNull(NavigationRecoveryCoordinator.ThreadKey("/explore/"));
    }
}
