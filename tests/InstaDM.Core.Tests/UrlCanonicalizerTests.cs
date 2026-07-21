using InstaDM.Core.Navigation;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class UrlCanonicalizerTests
{
    private static CanonicalUrl MustCanonicalize(string raw)
    {
        Assert.IsTrue(UrlCanonicalizer.TryCanonicalize(raw, out var url), $"Expected '{raw}' to canonicalize");
        return url!;
    }

    private static void MustFail(string? raw)
    {
        Assert.IsFalse(UrlCanonicalizer.TryCanonicalize(raw, out _), $"Expected '{raw}' to fail canonicalization");
    }

    [TestMethod]
    public void PlainInboxUrl_Canonicalizes()
    {
        var url = MustCanonicalize("https://www.instagram.com/direct/inbox/");
        Assert.AreEqual("www.instagram.com", url.Host);
        Assert.AreEqual("/direct/inbox/", url.Path);
    }

    [TestMethod]
    public void HostIsLowercased()
    {
        var url = MustCanonicalize("https://WWW.INSTAGRAM.COM/direct/inbox/");
        Assert.AreEqual("www.instagram.com", url.Host);
    }

    [TestMethod]
    public void TrailingHostDotIsRemoved()
    {
        var url = MustCanonicalize("https://www.instagram.com./direct/inbox/");
        Assert.AreEqual("www.instagram.com", url.Host);
    }

    [TestMethod]
    public void EmptyPathBecomesRoot()
    {
        var url = MustCanonicalize("https://www.instagram.com");
        Assert.AreEqual("/", url.Path);
    }

    [TestMethod]
    public void QueryAndFragmentAreDroppedFromPath()
    {
        var url = MustCanonicalize("https://www.instagram.com/direct/inbox/?utm=1#frag");
        Assert.AreEqual("/direct/inbox/", url.Path);
    }

    [TestMethod]
    public void ExplicitDefaultPortIsAccepted()
    {
        var url = MustCanonicalize("https://www.instagram.com:443/direct/inbox/");
        Assert.AreEqual("www.instagram.com", url.Host);
    }

    // ---- rejection table ----

    [TestMethod]
    [DataRow(null, DisplayName = "null")]
    [DataRow("", DisplayName = "empty")]
    [DataRow("   ", DisplayName = "whitespace")]
    [DataRow("not a url", DisplayName = "not a URL")]
    [DataRow("/direct/inbox/", DisplayName = "relative URL")]
    [DataRow("http://www.instagram.com/direct/inbox/", DisplayName = "http downgrade")]
    [DataRow("javascript:alert(1)", DisplayName = "javascript scheme")]
    [DataRow("data:text/html,<h1>x</h1>", DisplayName = "data scheme")]
    [DataRow("file:///etc/passwd", DisplayName = "file scheme")]
    [DataRow("ftp://www.instagram.com/", DisplayName = "ftp scheme")]
    [DataRow("intent://x#Intent;end", DisplayName = "intent scheme")]
    [DataRow("HTTPS://user@www.instagram.com/", DisplayName = "userinfo smuggling")]
    [DataRow("https://www.instagram.com@evil.com/direct/inbox/", DisplayName = "instagram-as-userinfo")]
    [DataRow("https://user:pass@www.instagram.com/", DisplayName = "user:pass")]
    [DataRow("https://www.instagram.com:8443/direct/inbox/", DisplayName = "non-default port")]
    [DataRow("https://www.instagram.com/direct%2Finbox/", DisplayName = "encoded slash")]
    [DataRow("https://www.instagram.com/p%2Ffoo", DisplayName = "encoded slash after /p")]
    [DataRow("https://xn--nstagram-e1a.com/direct/inbox/", DisplayName = "punycode lookalike host")]
    [DataRow("https://www.\u0456nstagram.com/", DisplayName = "unicode lookalike host")]
    [DataRow("https://www..instagram.com/", DisplayName = "empty host label")]
    public void HostileOrMalformedInputs_FailClosed(string? raw)
    {
        MustFail(raw);
    }

    // .NET's Uri resolves dot segments (literal and %2e-encoded) per
    // RFC 3986 before AbsolutePath is read — the same resolution the browser
    // applies before navigating. The policy therefore judges the *resolved*
    // destination, which is the page that would actually load. These tests
    // pin that resolution so a framework change cannot silently alter it.

    [TestMethod]
    public void LiteralDotDotSegments_ResolveBeforeJudgment()
    {
        var url = MustCanonicalize("https://www.instagram.com/direct/../");
        Assert.AreEqual("/", url.Path); // resolves to home feed => blocked by policy
    }

    [TestMethod]
    public void EncodedDotDotSegments_ResolveBeforeJudgment()
    {
        var url = MustCanonicalize("https://www.instagram.com/direct/%2e%2e/");
        Assert.AreEqual("/", url.Path);
    }

    [TestMethod]
    public void SingleDotSegment_ResolvesToRealPath()
    {
        var url = MustCanonicalize("https://www.instagram.com/./direct/inbox/");
        Assert.AreEqual("/direct/inbox/", url.Path);
    }

    [TestMethod]
    public void BackslashInPath_FailsClosed()
    {
        // Uri normalizes raw backslashes on some frameworks; construct via
        // escaped form to be sure the guard holds either way.
        Assert.IsFalse(
            UrlCanonicalizer.TryCanonicalize("https://www.instagram.com/direct%5Cinbox", out _),
            "escaped backslash must fail");
    }
}
