using InstaDM.Core.Navigation;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class PathMatcherTests
{
    [TestMethod]
    [DataRow("/p", "/p", true, DisplayName = "exact match")]
    [DataRow("/p/", "/p", true, DisplayName = "trailing slash")]
    [DataRow("/p/abc123", "/p", true, DisplayName = "child path")]
    [DataRow("/profile", "/p", false, DisplayName = "/p must not match /profile")]
    [DataRow("/privacy", "/p", false, DisplayName = "/p must not match /privacy")]
    [DataRow("/direct", "/direct", true, DisplayName = "bare direct exact")]
    [DataRow("/directory", "/direct", false, DisplayName = "/direct must not match /directory")]
    [DataRow("/direct/inbox", "/direct/inbox", true, DisplayName = "inbox exact")]
    [DataRow("/direct/inbox/", "/direct/inbox", true, DisplayName = "inbox slash")]
    [DataRow("/direct/inboxx", "/direct/inbox", false, DisplayName = "inbox suffix junk")]
    [DataRow("/api", "/api", true, DisplayName = "api exact")]
    [DataRow("/api-status", "/api", false, DisplayName = "/api must not match /api-status")]
    [DataRow("/accounts/login", "/accounts/login", true, DisplayName = "login exact")]
    [DataRow("/accounts/login2", "/accounts/login", false, DisplayName = "login suffix junk")]
    [DataRow("/P", "/p", false, DisplayName = "case sensitive (uppercase blocked upstream)")]
    [DataRow("/", "/p", false, DisplayName = "root shorter than prefix")]
    [DataRow("", "/p", false, DisplayName = "empty path")]
    public void DirectoryBoundaryMatching(string path, string prefix, bool expected)
    {
        Assert.AreEqual(expected, PathMatcher.Matches(path, prefix));
    }

    [TestMethod]
    public void MatchesAny_UsesSameBoundaryRule()
    {
        string[] prefixes = ["/direct/inbox", "/direct/t", "/direct/new"];
        Assert.IsTrue(PathMatcher.MatchesAny("/direct/t/12345/", prefixes));
        Assert.IsFalse(PathMatcher.MatchesAny("/direct/threads", prefixes));
        Assert.IsFalse(PathMatcher.MatchesAny("/direct", prefixes));
    }
}
