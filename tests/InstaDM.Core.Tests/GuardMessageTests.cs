using InstaDM.Core.WebHost;

namespace InstaDM.Core.Tests;

/// <summary>
/// The web-message bridge is a security boundary: any page script can call
/// postMessage. Only the exact guard schema may parse.
/// </summary>
[TestClass]
public sealed class GuardMessageTests
{
    private const string Valid =
        """{"v":1,"source":"instadm-guard","kind":"blockedClick","surface":"post"}""";

    [TestMethod]
    public void ValidGuardReport_Parses()
    {
        Assert.IsTrue(GuardMessage.TryParse(Valid, out var message));
        Assert.AreEqual(GuardMessageKind.BlockedClick, message!.Kind);
        Assert.AreEqual(GuardReportedSurface.Post, message.Surface);
    }

    [TestMethod]
    public void AllKindAndSurfaceValues_RoundTrip()
    {
        (string kind, GuardMessageKind expected)[] kinds =
        [
            ("blockedClick", GuardMessageKind.BlockedClick),
            ("blockedHistory", GuardMessageKind.BlockedHistory),
            ("guardInactive", GuardMessageKind.GuardInactive),
        ];
        (string surface, GuardReportedSurface expected)[] surfaces =
        [
            ("malformed", GuardReportedSurface.Malformed),
            ("offPlatform", GuardReportedSurface.OffPlatform),
            ("feed", GuardReportedSurface.Feed),
            ("explore", GuardReportedSurface.Explore),
            ("reels", GuardReportedSurface.Reels),
            ("stories", GuardReportedSurface.Stories),
            ("post", GuardReportedSurface.Post),
            ("directShell", GuardReportedSurface.DirectShell),
            ("other", GuardReportedSurface.Other),
        ];

        foreach (var (kind, expectedKind) in kinds)
        {
            foreach (var (surface, expectedSurface) in surfaces)
            {
                var json =
                    $$"""{"v":1,"source":"instadm-guard","kind":"{{kind}}","surface":"{{surface}}"}""";
                Assert.IsTrue(GuardMessage.TryParse(json, out var message), json);
                Assert.AreEqual(expectedKind, message!.Kind);
                Assert.AreEqual(expectedSurface, message.Surface);
            }
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "null")]
    [DataRow("", DisplayName = "empty")]
    [DataRow("not json", DisplayName = "not JSON")]
    [DataRow("[1,2,3]", DisplayName = "array")]
    [DataRow("\"string\"", DisplayName = "bare string")]
    [DataRow("""{"v":1,"source":"instadm-guard","kind":"blockedClick"}""",
        DisplayName = "missing surface")]
    [DataRow("""{"v":2,"source":"instadm-guard","kind":"blockedClick","surface":"post"}""",
        DisplayName = "wrong version")]
    [DataRow("""{"v":"1","source":"instadm-guard","kind":"blockedClick","surface":"post"}""",
        DisplayName = "version as string")]
    [DataRow("""{"v":1,"source":"instagram","kind":"blockedClick","surface":"post"}""",
        DisplayName = "wrong source")]
    [DataRow("""{"v":1,"source":"instadm-guard","kind":"navigate","surface":"post"}""",
        DisplayName = "unknown kind")]
    [DataRow("""{"v":1,"source":"instadm-guard","kind":"blockedClick","surface":"https://evil.com"}""",
        DisplayName = "URL smuggled as surface")]
    [DataRow("""{"v":1,"source":"instadm-guard","kind":"blockedClick","surface":"post","extra":"x"}""",
        DisplayName = "extra key rejected")]
    public void NonConformingMessages_AreRejected(string? json)
    {
        Assert.IsFalse(GuardMessage.TryParse(json, out _));
    }

    [TestMethod]
    public void OversizedPayload_IsRejectedWithoutParsing()
    {
        var padded = Valid[..^1] + new string(' ', GuardMessage.MaxLength) + "}";
        Assert.IsFalse(GuardMessage.TryParse(padded, out _));
    }

    [TestMethod]
    public void GuardJsSchema_MatchesParserExpectations()
    {
        // The guard posts {v, source, kind, surface} (guard.test.js pins the
        // key set). This test pins the same contract from the native side so
        // the two suites cannot drift apart silently.
        Assert.AreEqual(1, GuardMessage.SchemaVersion);
        Assert.AreEqual("instadm-guard", GuardMessage.ExpectedSource);
    }
}
