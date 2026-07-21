using InstaDM.Core.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InstaDM.App.Views;

public sealed partial class SettingsView : UserControl
{
    private AppSettings _settings = new();
    private bool _suppress;

    public event EventHandler? SettingsChanged;
    public event EventHandler? ClearInstagramDataRequested;
    public event EventHandler? ResetSettingsRequested;

    public SettingsView()
    {
        InitializeComponent();
    }

    public void Bind(AppSettings settings)
    {
        _suppress = true;
        _settings = settings;
        SelectTag(appearanceBox, settings.Appearance.ToString());
        SelectTag(notificationBox, settings.NotificationLevel.ToString());
        SelectTag(pollIntervalBox, settings.PollIntervalSeconds.ToString());
        followRequestsToggle.IsOn = settings.FollowRequestsEnabled;
        externalLinksToggle.IsOn = settings.OpenLinksInExternalBrowser;
        _suppress = false;
    }

    private static void SelectTag(ComboBox box, string tag)
    {
        foreach (var item in box.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.Ordinal))
            {
                box.SelectedItem = item;
                return;
            }
        }
    }

    private void OnAppearanceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppress || appearanceBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        if (Enum.TryParse<AppearancePreference>(item.Tag as string, out var value))
        {
            _settings.Appearance = value;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnNotificationChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppress || notificationBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        if (Enum.TryParse<NotificationLevel>(item.Tag as string, out var value))
        {
            _settings.NotificationLevel = value;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPollIntervalChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppress || pollIntervalBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        if (int.TryParse(item.Tag as string, out var seconds))
        {
            _settings.PollIntervalSeconds = seconds;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnFollowRequestsToggled(object sender, RoutedEventArgs e)
    {
        if (_suppress)
        {
            return;
        }

        _settings.FollowRequestsEnabled = followRequestsToggle.IsOn;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnExternalLinksToggled(object sender, RoutedEventArgs e)
    {
        if (_suppress)
        {
            return;
        }

        _settings.OpenLinksInExternalBrowser = externalLinksToggle.IsOn;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private async void OnClearInstagramData(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Clear Instagram data?",
            Content = "This signs you out of Instagram inside InstaDM and deletes this app's private browser cookies and cache. Other browsers are not affected.",
            PrimaryButtonText = "Clear and sign out",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ClearInstagramDataRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private async void OnResetSettings(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Reset app settings?",
            Content = "Restores appearance, notifications, and feature toggles to defaults. Does not clear your Instagram session.",
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ResetSettingsRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
