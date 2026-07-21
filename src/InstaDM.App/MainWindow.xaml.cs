using Microsoft.UI.Xaml;

namespace InstaDM.App;

/// <summary>
/// The single main window. Minimum size ~800x600 (per the source app's
/// baseline; verified on Windows during M9).
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Title = "DMs";
    }
}
