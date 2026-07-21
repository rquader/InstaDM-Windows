using InstaDM.Core.Notifications;
using InstaDM.Core.Settings;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class UnreadTitleParserTests
{
    [TestMethod]
    public void ParsesParenthesizedCount()
    {
        Assert.AreEqual(3, UnreadTitleParser.TryParse("(3) Inbox • Instagram"));
        Assert.AreEqual(0, UnreadTitleParser.TryParse("Inbox • Instagram"));
        Assert.AreEqual(1234, UnreadTitleParser.TryParse("(1,234) Instagram"));
    }

    [TestMethod]
    public void LeadingParenWithoutClose_IsNil_NotZero()
    {
        Assert.IsNull(UnreadTitleParser.TryParse("(3 new messages Instagram"));
    }

    [TestMethod]
    public void Unrecognized_IsNil()
    {
        Assert.IsNull(UnreadTitleParser.TryParse("Something else"));
        Assert.IsNull(UnreadTitleParser.TryParse(null));
    }
}

[TestClass]
public sealed class UnreadStateMachineTests
{
    [TestMethod]
    public void FirstObservation_SetsBaseline_NoBanner()
    {
        var machine = new UnreadStateMachine();
        var decision = machine.Observe(5, NotificationLevel.Standard);
        Assert.AreEqual(UnreadNotificationAction.UpdateBadge, decision.Action);
        Assert.IsNull(decision.AddedCount);
        Assert.AreEqual(5, machine.Baseline);
    }

    [TestMethod]
    public void Increase_ShowsGenericBanner()
    {
        var machine = new UnreadStateMachine();
        machine.Observe(2, NotificationLevel.Standard);
        var decision = machine.Observe(5, NotificationLevel.Standard);
        Assert.AreEqual(UnreadNotificationAction.ShowGenericBanner, decision.Action);
        Assert.AreEqual(3, decision.AddedCount);
    }

    [TestMethod]
    public void Increase_WhileSuppressed_UpdatesBadgeOnly()
    {
        var machine = new UnreadStateMachine();
        machine.Observe(1, NotificationLevel.Standard);
        machine.SetBannerSuppressed(true);
        var decision = machine.Observe(4, NotificationLevel.Standard);
        Assert.AreEqual(UnreadNotificationAction.UpdateBadge, decision.Action);
        Assert.AreEqual(3, decision.AddedCount);
    }

    [TestMethod]
    public void BadgeLevel_NeverShowsBanner()
    {
        var machine = new UnreadStateMachine();
        machine.Observe(1, NotificationLevel.Badge);
        var decision = machine.Observe(2, NotificationLevel.Badge);
        Assert.AreEqual(UnreadNotificationAction.UpdateBadge, decision.Action);
    }

    [TestMethod]
    public void Off_DoesNothing()
    {
        var machine = new UnreadStateMachine();
        Assert.AreEqual(UnreadNotificationAction.None, machine.Observe(9, NotificationLevel.Off).Action);
    }

    [TestMethod]
    public void NilParse_LeavesStateAlone()
    {
        var machine = new UnreadStateMachine();
        machine.Observe(3, NotificationLevel.Standard);
        var decision = machine.Observe(null, NotificationLevel.Standard);
        Assert.AreEqual(UnreadNotificationAction.None, decision.Action);
        Assert.AreEqual(3, machine.LastCount);
    }

    [TestMethod]
    public void Decrease_LowersBaseline_WithoutBanner()
    {
        var machine = new UnreadStateMachine();
        machine.Observe(5, NotificationLevel.Standard);
        var decision = machine.Observe(2, NotificationLevel.Standard);
        Assert.AreEqual(UnreadNotificationAction.UpdateBadge, decision.Action);
        Assert.AreEqual(2, machine.Baseline);
    }
}
