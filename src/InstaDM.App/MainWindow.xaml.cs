using InstaDM.Core.Lifecycle;
using InstaDM.Core.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace InstaDM.App;

/// <summary>
/// Single main window: Messages + Settings. Closing it shuts down owned
/// timers/watchers and ends the process — no tray residual.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly LocalSettingsStore _store;
    private readonly AppLifecycleCoordinator _lifecycle;
    private AppSettings _settings;

    public MainWindow(LocalSettingsStore store, AppLifecycleCoordinator lifecycle, AppSettings settings)
    {
        _store = store;
        _lifecycle = lifecycle;
        _settings = settings;

        InitializeComponent();
        AppWindow.Title = "InstaDM";
        AppWindow.Resize(new SizeInt32(1100, 760));
        ApplyMinSize();
        ApplyAppearance(_settings.Appearance);
        RequestsItem.Visibility = _settings.FollowRequestsEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;

        SettingsPane.Bind(_settings);
        SettingsPane.SettingsChanged += OnSettingsChanged;
        SettingsPane.ClearInstagramDataRequested += async (_, _) =>
            await MessagesHost.ClearInstagramDataAsync();
        SettingsPane.ResetSettingsRequested += OnResetSettings;

        Closed += OnClosed;
    }

    private void ApplyMinSize()
    {
        // ~800x600 baseline from the macOS app; WinUI OverlappedPresenter.
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 800;
            presenter.PreferredMinimumHeight = 600;
        }
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        var showSettings = tag == "settings";
        SettingsPane.Visibility = showSettings ? Visibility.Visible : Visibility.Collapsed;
        MessagesHost.Visibility = showSettings ? Visibility.Collapsed : Visibility.Visible;

        if (tag == "requests" && _settings.FollowRequestsEnabled)
        {
            MessagesHost.NavigateToFollowRequests();
            MessagesHost.Visibility = Visibility.Visible;
            SettingsPane.Visibility = Visibility.Collapsed;
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        _store.Save(_settings);
        ApplyAppearance(_settings.Appearance);
        RequestsItem.Visibility = _settings.FollowRequestsEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnResetSettings(object? sender, EventArgs e)
    {
        _store.ResetToDefaults();
        _settings = _store.Load();
        SettingsPane.Bind(_settings);
        ApplyAppearance(_settings.Appearance);
        RequestsItem.Visibility = Visibility.Collapsed;
    }

    private void ApplyAppearance(AppearancePreference preference)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = preference switch
            {
                AppearancePreference.Light => ElementTheme.Light,
                AppearancePreference.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _lifecycle.Shutdown();
        // Closing the last window ends the app (no tray).
        Application.Current.Exit();
    }
}
