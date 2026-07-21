using InstaDM.Core.Navigation;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class NavigationPolicyTests
{
    private static readonly NavigationPolicy Default = new();
    private static readonly NavigationPolicy WithFollowRequests =
        new(new PolicyOptions { FollowRequestsEnabled = true });
    private static readonly NavigationPolicy WithSharedPosts =
        new(new PolicyOptions { SharedPostsEnabled = true });

    // ------------------------------------------------------------------
    // Classification
    // ------------------------------------------------------------------

    [TestMethod]
    [DataRow("https://www.instagram.com/direct/inbox/", InstagramSurface.DirectInbox)]
    [DataRow("https://www.instagram.com/direct/t/1234567890/", InstagramSurface.DirectThread)]
    [DataRow("https://www.instagram.com/direct/new/", InstagramSurface.DirectNew)]
    [DataRow("https://www.instagram.com/direct/", InstagramSurface.DirectShell)]
    [DataRow("https://www.instagram.com/direct", InstagramSurface.DirectShell)]
    [DataRow("https://www.instagram.com/", InstagramSurface.HomeFeed)]
    [DataRow("https://www.instagram.com/accounts/login/", InstagramSurface.AuthAccount)]
    [DataRow("https://www.instagram.com/accounts/onetap/", InstagramSurface.AuthAccount)]
    [DataRow("https://www.instagram.com/accounts/password/reset/", InstagramSurface.AuthAccount)]
    [DataRow("https://www.instagram.com/accounts/two_factor/", InstagramSurface.AuthAccount)]
    [DataRow("https://www.instagram.com/accounts/logout/", InstagramSurface.AuthAccount)]
    [DataRow("https://www.instagram.com/challenge/12345/", InstagramSurface.AuthChallenge)]
    [DataRow("https://www.instagram.com/auth_platform/codeentry/", InstagramSurface.AuthPlatform)]
    [DataRow("https://accounts.instagram.com/anything/at/all/", InstagramSurface.AuthHost)]
    [DataRow("https://www.instagram.com/api/v1/direct_v2/inbox/", InstagramSurface.InternalEndpoint)]
    [DataRow("https://www.instagram.com/graphql/query/", InstagramSurface.InternalEndpoint)]
    [DataRow("https://www.instagram.com/ajax/bz", InstagramSurface.InternalEndpoint)]
    [DataRow("https://www.instagram.com/static/bundles/x.js", InstagramSurface.InternalEndpoint)]
    [DataRow("https://www.instagram.com/explore/", InstagramSurface.Explore)]
    [DataRow("https://www.instagram.com/explore/tags/cats/", InstagramSurface.Explore)]
    [DataRow("https://www.instagram.com/reels/", InstagramSurface.Reels)]
    [DataRow("https://www.instagram.com/reel/Cabc123/", InstagramSurface.Reels)]
    [DataRow("https://www.instagram.com/stories/someuser/123/", InstagramSurface.Stories)]
    [DataRow("https://www.instagram.com/p/Cabc123/", InstagramSurface.Post)]
    [DataRow("https://www.instagram.com/tv/Cabc123/", InstagramSurface.Post)]
    [DataRow("https://www.instagram.com/some_user.name/", InstagramSurface.Profile)]
    [DataRow("https://www.instagram.com/accounts/activity/", InstagramSurface.FollowRequests)]
    [DataRow("https://www.instagram.com/accounts/edit/", InstagramSurface.UnknownInstagram)]
    [DataRow("https://www.instagram.com/legal/terms/", InstagramSurface.UnknownInstagram)]
    [DataRow("https://evil.com/direct/inbox/", InstagramSurface.OffPlatform)]
    [DataRow("https://www.instagram.com.evil.com/direct/inbox/", InstagramSurface.OffPlatform)]
    [DataRow("https://wwwinstagram.com/", InstagramSurface.OffPlatform)]
    [DataRow("https://instagram.com.co/", InstagramSurface.OffPlatform)]
    [DataRow("http://www.instagram.com/direct/inbox/", InstagramSurface.Malformed)]
    [DataRow("javascript:void(0)", InstagramSurface.Malformed)]
    [DataRow("", InstagramSurface.Malformed)]
    public void Classify(string url, InstagramSurface expected)
    {
        Assert.AreEqual(expected, Default.Classify(url), url);
    }

    [TestMethod]
    public void Classify_MixedCaseHost_StillClassified()
    {
        Assert.AreEqual(InstagramSurface.DirectInbox,
            Default.Classify("https://WWW.Instagram.COM/direct/inbox/"));
    }

    [TestMethod]
    public void Classify_UppercasePath_IsUnknownNotDm()
    {
        // Path case is preserved; Instagram's canonical paths are lowercase,
        // so /DIRECT/INBOX must not classify as a DM surface.
        Assert.AreNotEqual(InstagramSurface.DirectInbox,
            Default.Classify("https://www.instagram.com/DIRECT/INBOX/"));
    }

    // ------------------------------------------------------------------
    // Network-request layer, defaults
    // ------------------------------------------------------------------

    [TestMethod]
    [DataRow("https://www.instagram.com/direct/inbox/", true, DecisionReason.AllowedDmSurface)]
    [DataRow("https://www.instagram.com/direct/t/999/", true, DecisionReason.AllowedDmSurface)]
    [DataRow("https://www.instagram.com/direct/new/", true, DecisionReason.AllowedDmSurface)]
    [DataRow("https://www.instagram.com/direct/", false, DecisionReason.BlockedDirectShell)]
    [DataRow("https://www.instagram.com/accounts/login/", true, DecisionReason.AllowedAuthSurface)]
    [DataRow("https://www.instagram.com/challenge/x/", true, DecisionReason.AllowedAuthSurface)]
    [DataRow("https://www.instagram.com/auth_platform/x/", true, DecisionReason.AllowedAuthSurface)]
    [DataRow("https://accounts.instagram.com/login/", true, DecisionReason.AllowedAuthHost)]
    [DataRow("https://www.instagram.com/api/v1/x/", true, DecisionReason.AllowedInternalEndpoint)]
    [DataRow("https://www.instagram.com/", false, DecisionReason.BlockedHomeFeed)]
    [DataRow("https://www.instagram.com/explore/", false, DecisionReason.BlockedExplore)]
    [DataRow("https://www.instagram.com/stories/u/1/", false, DecisionReason.BlockedStories)]
    [DataRow("https://www.instagram.com/someuser/", false, DecisionReason.BlockedProfile)]
    [DataRow("https://www.instagram.com/p/C1/", false, DecisionReason.BlockedSharedPostsDisabled)]
    [DataRow("https://www.instagram.com/reel/C1/", false, DecisionReason.BlockedSharedPostsDisabled)]
    [DataRow("https://www.instagram.com/accounts/activity/", false, DecisionReason.BlockedFollowRequestsDisabled)]
    [DataRow("https://www.instagram.com/accounts/edit/", false, DecisionReason.BlockedUnknownSurface)]
    [DataRow("https://evil.com/", false, DecisionReason.BlockedOffPlatform)]
    [DataRow("http://www.instagram.com/direct/inbox/", false, DecisionReason.BlockedMalformed)]
    public void DecideNetworkRequest_Defaults(string url, bool allowed, DecisionReason reason)
    {
        var decision = Default.DecideNetworkRequest(url);
        Assert.AreEqual(allowed, decision.IsAllowed, url);
        Assert.AreEqual(reason, decision.Reason, url);
    }

    // ------------------------------------------------------------------
    // Feature gating
    // ------------------------------------------------------------------

    [TestMethod]
    public void FollowRequests_EnabledAllows_DisabledBlocks()
    {
        const string url = "https://www.instagram.com/accounts/activity/";
        Assert.IsFalse(Default.DecideNetworkRequest(url).IsAllowed);
        var enabled = WithFollowRequests.DecideNetworkRequest(url);
        Assert.IsTrue(enabled.IsAllowed);
        Assert.AreEqual(DecisionReason.AllowedFollowRequests, enabled.Reason);
    }

    [TestMethod]
    public void SharedPosts_RequiresBothFlagAndDmSource()
    {
        const string post = "https://www.instagram.com/p/Cabc/";

        // flag off, any source: blocked
        Assert.AreEqual(DecisionReason.BlockedSharedPostsDisabled,
            Default.DecideNetworkRequest(post, NavigationContext.FromDm).Reason);

        // flag on, unknown source: still blocked (source-gated)
        Assert.AreEqual(DecisionReason.BlockedSharedPostNotFromDm,
            WithSharedPosts.DecideNetworkRequest(post, NavigationContext.None).Reason);

        // flag on, from a DM: allowed
        var allowed = WithSharedPosts.DecideNetworkRequest(post, NavigationContext.FromDm);
        Assert.IsTrue(allowed.IsAllowed);
        Assert.AreEqual(DecisionReason.AllowedSharedPostFromDm, allowed.Reason);
    }

    [TestMethod]
    public void SharedPosts_ReelsAndTv_FollowSameGating()
    {
        foreach (var url in new[]
        {
            "https://www.instagram.com/reel/C1/",
            "https://www.instagram.com/tv/C1/",
        })
        {
            Assert.IsTrue(WithSharedPosts.DecideNetworkRequest(url, NavigationContext.FromDm).IsAllowed, url);
            Assert.IsFalse(WithSharedPosts.DecideNetworkRequest(url, NavigationContext.None).IsAllowed, url);
        }
    }

    // ------------------------------------------------------------------
    // Main-document gate
    // ------------------------------------------------------------------

    [TestMethod]
    [DataRow("https://www.instagram.com/direct/inbox/", true)]
    [DataRow("https://www.instagram.com/direct/t/1/", true)]
    [DataRow("https://www.instagram.com/accounts/login/", true)]
    [DataRow("https://www.instagram.com/challenge/x/", true)]
    [DataRow("https://accounts.instagram.com/x/", true)]
    [DataRow("https://www.instagram.com/api/v1/x/", false, DisplayName = "internal endpoint may load but not persist")]
    [DataRow("https://www.instagram.com/graphql/query/", false)]
    [DataRow("https://www.instagram.com/", false)]
    [DataRow("https://www.instagram.com/direct/", false)]
    [DataRow("https://www.instagram.com/someuser/", false)]
    public void IsUserSurface_Defaults(string url, bool expected)
    {
        Assert.AreEqual(expected, Default.IsUserSurface(url), url);
    }

    // ------------------------------------------------------------------
    // Profile detection
    // ------------------------------------------------------------------

    [TestMethod]
    [DataRow("/someuser/", true)]
    [DataRow("/some_user.name/", true)]
    [DataRow("/user123/", true)]
    [DataRow("/explore/", false, DisplayName = "reserved word")]
    [DataRow("/direct/", false)]
    [DataRow("/p/", false)]
    [DataRow("/accounts/", false)]
    [DataRow("/user/extra/", false, DisplayName = "two segments is not a profile")]
    [DataRow("/", false)]
    [DataRow("/user name/", false, DisplayName = "invalid username chars")]
    public void IsProfilePath(string path, bool expected)
    {
        Assert.AreEqual(expected, NavigationPolicy.IsProfilePath(path), path);
    }

    // ------------------------------------------------------------------
    // Helpers used by recovery
    // ------------------------------------------------------------------

    [TestMethod]
    [DataRow("/direct/inbox", true)]
    [DataRow("/direct/inbox/", true)]
    [DataRow("/direct/t/123", true)]
    [DataRow("/direct/new", true)]
    [DataRow("/direct", false, DisplayName = "bare /direct is not DM")]
    [DataRow("/direct/", false)]
    [DataRow("/direct/threads", false)]
    public void IsDirectMessagingPath(string path, bool expected)
    {
        Assert.AreEqual(expected, NavigationPolicy.IsDirectMessagingPath(path), path);
    }

    [TestMethod]
    [DataRow("/accounts/login", true)]
    [DataRow("/accounts/login/two_factor", true)]
    [DataRow("/challenge/123", true)]
    [DataRow("/auth_platform/codeentry", true)]
    [DataRow("/accounts/activity", false, DisplayName = "follow requests is not auth")]
    [DataRow("/accounts/edit", false)]
    [DataRow("/direct/inbox", false)]
    public void IsAuthSurfacePath(string path, bool expected)
    {
        Assert.AreEqual(expected, NavigationPolicy.IsAuthSurfacePath(path), path);
    }

    [TestMethod]
    [DataRow("https://www.instagram.com/", true, DisplayName = "feed prefetch")]
    [DataRow("https://www.instagram.com/explore/", true)]
    [DataRow("https://www.instagram.com/reels/", true)]
    [DataRow("https://www.instagram.com/accounts/manage/x/", true)]
    [DataRow("https://www.instagram.com/someuser/", true, DisplayName = "profile hover prefetch")]
    [DataRow("https://www.instagram.com/web/push/notifications/x/", true, DisplayName = "notifications chrome")]
    [DataRow("https://evil.com/x", true, DisplayName = "off-platform background noise")]
    [DataRow("https://www.instagram.com/direct/t/1/", false, DisplayName = "DM thread is never incidental")]
    [DataRow("https://www.instagram.com/accounts/login/", false, DisplayName = "auth is never incidental")]
    public void IsIncidentalBlockedPrefetch(string url, bool expected)
    {
        Assert.AreEqual(expected, Default.IsIncidentalBlockedPrefetch(url), url);
    }

    [TestMethod]
    [DataRow("https://www.instagram.com/", true)]
    [DataRow("https://www.instagram.com/direct/", true)]
    [DataRow("https://www.instagram.com/someuser/", true)]
    [DataRow("https://www.instagram.com/stories/u/1/", true)]
    [DataRow("https://evil.com/", true)]
    [DataRow("https://www.instagram.com/direct/inbox/", false)]
    [DataRow("https://www.instagram.com/accounts/login/", false)]
    [DataRow("https://www.instagram.com/challenge/x/", false)]
    public void ShouldRecoverFromMainDocument_Defaults(string url, bool expected)
    {
        Assert.AreEqual(expected, Default.ShouldRecoverFromMainDocument(url), url);
    }

    [TestMethod]
    public void ShouldRecover_SharedPostFromDm_NotWhenEnabled()
    {
        const string post = "https://www.instagram.com/p/C1/";
        Assert.IsTrue(Default.ShouldRecoverFromMainDocument(post, NavigationContext.FromDm));
        Assert.IsFalse(WithSharedPosts.ShouldRecoverFromMainDocument(post, NavigationContext.FromDm));
        Assert.IsTrue(WithSharedPosts.ShouldRecoverFromMainDocument(post, NavigationContext.None));
    }
}
