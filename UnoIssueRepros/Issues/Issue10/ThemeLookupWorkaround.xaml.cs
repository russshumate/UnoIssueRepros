using Microsoft.UI.Xaml.Media;

namespace UnoIssueRepros.Issues.Issue10;

public sealed partial class ThemeLookupWorkaround : UserControl
{
    public ThemeLookupWorkaround()
    {
        this.InitializeComponent();
        Loaded += (_, _) => Refresh();
        ThemedPanel.ActualThemeChanged += (_, _) => Refresh();
    }

    private void OnLight(object sender, RoutedEventArgs e) => ThemedPanel.RequestedTheme = ElementTheme.Light;

    private void OnDark(object sender, RoutedEventArgs e) => ThemedPanel.RequestedTheme = ElementTheme.Dark;

    private void OnDefault(object sender, RoutedEventArgs e) => ThemedPanel.RequestedTheme = ElementTheme.Default;

    private void Refresh()
    {
        var color = (Swatch.Background as SolidColorBrush)?.Color ?? default;
        SwatchText.Text = $"ActualTheme = {ThemedPanel.ActualTheme}. The swatch resolved to {ThemeLookupRepro.ColorName(color)}.";
    }
}
