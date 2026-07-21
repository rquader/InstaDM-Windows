using InstaDM.Core.WebHost;

namespace InstaDM.Core.Tests;

/// <summary>
/// Pins every ADR-006 privacy value. A change to any of these is a privacy
/// decision, not a refactor — it must break a test and force a deliberate
/// edit here plus an ADR update.
/// </summary>
[TestClass]
public sealed class WebViewHostConfigurationTests
{
    private static readonly WebViewHostConfiguration Release =
        WebViewHostConfiguration.Create("/local/appdata", isDebugBuild: false);

    private static readonly WebViewHostConfiguration Debug =
        WebViewHostConfiguration.Create("/local/appdata", isDebugBuild: true);

    [TestMethod]
    public void UserDataFolder_IsDedicatedAndAppExclusive()
    {
        StringAssert.EndsWith(Release.UserDataFolder,
            Path.Combine("InstaDM", "WebView2"));
        StringAssert.StartsWith(Release.UserDataFolder, "/local/appdata");
    }

    [TestMethod]
    public void BrowserArguments_DisableSmartScreenAndChromiumServices()
    {
        var args = Release.AdditionalBrowserArguments;
        StringAssert.Contains(args, "--disable-features=msSmartScreenProtection");
        StringAssert.Contains(args, "--disable-domain-reliability");
        StringAssert.Contains(args, "--disable-background-networking");
        StringAssert.Contains(args, "--disable-component-update");
    }

    [TestMethod]
    public void PrivacyInvariantValues_AreImmutable()
    {
        foreach (var config in new[] { Release, Debug })
        {
            Assert.IsFalse(config.IsReputationCheckingRequired, "SmartScreen must stay off");
            Assert.IsTrue(config.IsCustomCrashReportingEnabled, "crash dumps must stay local");
            Assert.IsFalse(config.IsPasswordAutosaveEnabled);
            Assert.IsFalse(config.IsGeneralAutofillEnabled);
            Assert.IsFalse(config.AreBrowserExtensionsEnabled);
            Assert.IsFalse(config.AllowSingleSignOnUsingOSPrimaryAccount);
            Assert.IsTrue(config.IsTrackingPreventionEnabled);
        }
    }

    [TestMethod]
    public void DevTools_DebugOnly()
    {
        Assert.IsTrue(Debug.AreDevToolsEnabled);
        Assert.IsFalse(Release.AreDevToolsEnabled);
    }

    [TestMethod]
    public void NoRemoteDebuggingSwitch_EverConfigured()
    {
        // The static privacy scanner also enforces this repo-wide; this test
        // pins the actual runtime value.
        Assert.IsFalse(Release.AdditionalBrowserArguments.Contains("remote-debugging"));
        Assert.IsFalse(Debug.AdditionalBrowserArguments.Contains("remote-debugging"));
    }

    [TestMethod]
    public void EmptyAppDataRoot_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            WebViewHostConfiguration.Create("", isDebugBuild: false));
        Assert.ThrowsExactly<ArgumentException>(() =>
            WebViewHostConfiguration.Create("   ", isDebugBuild: false));
    }
}
