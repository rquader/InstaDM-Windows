using InstaDM.Core.Lifecycle;
using InstaDM.Core.Navigation;
using InstaDM.Core.Settings;
using Microsoft.UI.Xaml;

namespace InstaDM.App;

/// <summary>
/// Application entry point. One window; closing it exits the process —
/// no tray icon, no startup task, no background service (see
/// docs/SOURCE_BEHAVIOR.md B12).
/// </summary>
public partial class App : Application
{
    private MainWindow? _window;

    public static new App Current => (App)Application.Current;

    public LocalSettingsStore SettingsStore { get; }
    public AppSettings Settings { get; private set; }
    public AppLifecycleCoordinator Lifecycle { get; } = new();

    public App()
    {
        InitializeComponent();

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InstaDM");
        SettingsStore = new LocalSettingsStore(dataDir);
        Settings = SettingsStore.Load();
    }

    public PolicyOptions CreatePolicyOptions() => new()
    {
        FollowRequestsEnabled = Settings.FollowRequestsEnabled,
        SharedPostsEnabled = false, // unavailable in first release
    };

    public void ReplaceSettings(AppSettings settings)
    {
        Settings = settings;
        SettingsStore.Save(settings);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow(SettingsStore, Lifecycle, Settings);
        _window.Activate();
    }
}
