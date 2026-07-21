using System.Text.Json;
using InstaDM.Core.Navigation;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class PolicyScriptBuilderTests
{
    [TestMethod]
    public void Payload_ContainsExpectedKeysAndValues()
    {
        var json = PolicyScriptBuilder.BuildPayloadJson(new NavigationPolicy());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual(PolicyScriptBuilder.PayloadVersion, root.GetProperty("version").GetInt32());
        Assert.AreEqual("accounts.instagram.com", root.GetProperty("authOnlyHost").GetString());
        Assert.AreEqual(NavigationPolicy.InboxUrl, root.GetProperty("inboxUrl").GetString());

        var hosts = root.GetProperty("allowedHosts").EnumerateArray().Select(e => e.GetString()).ToList();
        CollectionAssert.AreEquivalent(
            new[] { "www.instagram.com", "instagram.com", "accounts.instagram.com" }, hosts);

        var dm = root.GetProperty("dmPrefixes").EnumerateArray().Select(e => e.GetString()).ToList();
        CollectionAssert.AreEquivalent(new[] { "/direct/inbox", "/direct/t", "/direct/new" }, dm);

        var auth = root.GetProperty("authPrefixes").EnumerateArray().Select(e => e.GetString()).ToList();
        CollectionAssert.Contains(auth, "/accounts/login");
        CollectionAssert.Contains(auth, "/challenge");
        CollectionAssert.Contains(auth, "/auth_platform");
        CollectionAssert.DoesNotContain(auth, "/accounts/activity");
    }

    [TestMethod]
    public void Payload_DefaultOptions_ExportNoOptionalPrefixes()
    {
        var json = PolicyScriptBuilder.BuildPayloadJson(new NavigationPolicy());
        using var doc = JsonDocument.Parse(json);
        Assert.AreEqual(0, doc.RootElement.GetProperty("optionalPrefixes").GetArrayLength());
    }

    [TestMethod]
    public void Payload_FollowRequestsEnabled_ExportsActivityPrefix()
    {
        var policy = new NavigationPolicy(new PolicyOptions { FollowRequestsEnabled = true });
        var json = PolicyScriptBuilder.BuildPayloadJson(policy);
        using var doc = JsonDocument.Parse(json);
        var optional = doc.RootElement.GetProperty("optionalPrefixes")
            .EnumerateArray().Select(e => e.GetString()).ToList();
        CollectionAssert.AreEquivalent(new[] { "/accounts/activity" }, optional);
    }

    [TestMethod]
    public void Payload_SharedPostsEnabled_StillExportsNoPostPrefixes()
    {
        // Source-gating cannot be verified in JS, so shared-post prefixes are
        // deliberately never exported: the guard fails closed and the native
        // policy grants the DM-sourced exception.
        var policy = new NavigationPolicy(new PolicyOptions { SharedPostsEnabled = true });
        var json = PolicyScriptBuilder.BuildPayloadJson(policy);
        using var doc = JsonDocument.Parse(json);
        var optional = doc.RootElement.GetProperty("optionalPrefixes")
            .EnumerateArray().Select(e => e.GetString()).ToList();
        CollectionAssert.DoesNotContain(optional, "/p");
        CollectionAssert.DoesNotContain(optional, "/reel");
    }

    [TestMethod]
    public void Payload_ContainsNoSensitiveKeys()
    {
        var json = PolicyScriptBuilder.BuildPayloadJson(
            new NavigationPolicy(new PolicyOptions { FollowRequestsEnabled = true, SharedPostsEnabled = true }));
        // "/accounts/password" is a legitimate auth *path prefix*; the scan
        // targets credential/session material markers, not the word password.
        foreach (var forbidden in new[] { "cookie", "sessionid", "csrftoken", "authorization", "bearer" })
        {
            Assert.IsFalse(json.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                $"payload must not contain '{forbidden}'");
        }
    }

    [TestMethod]
    public void InjectIntoScript_ReplacesPlaceholder()
    {
        const string template = "const policy = __INSTADM_POLICY__;";
        var script = PolicyScriptBuilder.InjectIntoScript(template, new NavigationPolicy());
        Assert.IsFalse(script.Contains(PolicyScriptBuilder.PolicyPlaceholder));
        StringAssert.StartsWith(script, "const policy = {");
    }

    [TestMethod]
    public void InjectIntoScript_MissingPlaceholder_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            PolicyScriptBuilder.InjectIntoScript("no placeholder here", new NavigationPolicy()));
    }

    [TestMethod]
    public void JsMirror_DirectoryBoundarySemantics_MatchPathMatcher()
    {
        // The JS guard mirrors PathMatcher with:
        //   path === prefix || path.startsWith(prefix + "/")
        // Prove the C# matcher agrees with that exact formulation across a
        // adversarial sample so the two implementations cannot drift.
        (string path, string prefix)[] cases =
        [
            ("/p", "/p"), ("/p/", "/p"), ("/p/abc", "/p"),
            ("/profile", "/p"), ("/privacy", "/p"),
            ("/direct", "/direct"), ("/directory", "/direct"),
            ("/direct/inbox", "/direct/inbox"), ("/direct/inboxx", "/direct/inbox"),
            ("/api", "/api"), ("/api-status", "/api"),
            ("", "/p"), ("/", "/"),
        ];

        foreach (var (path, prefix) in cases)
        {
            var jsMirror = path == prefix || path.StartsWith(prefix + "/", StringComparison.Ordinal);
            Assert.AreEqual(jsMirror, PathMatcher.Matches(path, prefix), $"drift at ({path}, {prefix})");
        }
    }
}
