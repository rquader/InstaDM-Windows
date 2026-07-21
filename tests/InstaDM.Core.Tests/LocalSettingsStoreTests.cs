using InstaDM.Core.Settings;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class LocalSettingsStoreTests
{
    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "instadm-settings-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [TestMethod]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var dir = TempDir();
        var store = new LocalSettingsStore(dir);
        var settings = store.Load();
        Assert.AreEqual(AppearancePreference.System, settings.Appearance);
        Assert.AreEqual(NotificationLevel.Standard, settings.NotificationLevel);
        Assert.AreEqual(30, settings.PollIntervalSeconds);
        Assert.IsFalse(settings.FollowRequestsEnabled);
        Assert.IsFalse(settings.OpenLinksInExternalBrowser);
    }

    [TestMethod]
    public void Save_ThenLoad_RoundTrips()
    {
        var dir = TempDir();
        var store = new LocalSettingsStore(dir);
        var settings = new AppSettings
        {
            Appearance = AppearancePreference.Dark,
            NotificationLevel = NotificationLevel.Badge,
            PollIntervalSeconds = 120,
            FollowRequestsEnabled = true,
            OpenLinksInExternalBrowser = false,
        };
        store.Save(settings);

        var loaded = store.Load();
        Assert.AreEqual(AppearancePreference.Dark, loaded.Appearance);
        Assert.AreEqual(NotificationLevel.Badge, loaded.NotificationLevel);
        Assert.AreEqual(120, loaded.PollIntervalSeconds);
        Assert.IsTrue(loaded.FollowRequestsEnabled);
        Assert.IsFalse(loaded.OpenLinksInExternalBrowser);
    }

    [TestMethod]
    public void Normalize_ClampsInvalidPollInterval()
    {
        var settings = new AppSettings { PollIntervalSeconds = 99 };
        settings.Normalize();
        Assert.AreEqual(AppSettings.DefaultPollIntervalSeconds, settings.PollIntervalSeconds);
    }

    [TestMethod]
    public void ResetToDefaults_RemovesFile()
    {
        var dir = TempDir();
        var store = new LocalSettingsStore(dir);
        store.Save(new AppSettings { Appearance = AppearancePreference.Light });
        Assert.IsTrue(File.Exists(store.FilePath));
        store.ResetToDefaults();
        Assert.IsFalse(File.Exists(store.FilePath));
        Assert.AreEqual(AppearancePreference.System, store.Load().Appearance);
    }

    [TestMethod]
    public void File_ContainsNoSecretFieldNames()
    {
        var dir = TempDir();
        var store = new LocalSettingsStore(dir);
        store.Save(new AppSettings());
        var json = File.ReadAllText(store.FilePath).ToLowerInvariant();
        foreach (var banned in new[] { "sessionid", "cookie", "password", "token", "csrf", "message", "username" })
        {
            Assert.IsFalse(json.Contains(banned), banned);
        }
    }
}

[TestClass]
public sealed class AppLifecycleCoordinatorTests
{
    private sealed class Flag : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    [TestMethod]
    public void Shutdown_DisposesOwnedResources_Idempotent()
    {
        var life = new Lifecycle.AppLifecycleCoordinator();
        var a = new Flag();
        var b = new Flag();
        life.Own(a);
        life.Own(b);
        life.Shutdown();
        life.Shutdown();
        Assert.IsTrue(a.Disposed);
        Assert.IsTrue(b.Disposed);
        Assert.IsTrue(life.IsShuttingDown);
    }

    [TestMethod]
    public void Own_AfterShutdown_DisposesImmediately()
    {
        var life = new Lifecycle.AppLifecycleCoordinator();
        life.Shutdown();
        var flag = new Flag();
        life.Own(flag);
        Assert.IsTrue(flag.Disposed);
    }
}
