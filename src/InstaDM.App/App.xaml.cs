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

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
