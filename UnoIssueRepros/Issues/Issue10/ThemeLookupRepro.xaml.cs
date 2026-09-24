using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;

// Uno0001 ("not implemented in Uno") is expected: calling AccessibilitySettings.HighContrast is the repro.
#pragma warning disable Uno0001

namespace UnoIssueRepros.Issues.Issue10;

public sealed partial class ThemeLookupRepro : UserControl
{
    public ThemeLookupRepro()
    {
        this.InitializeComponent();
        Loaded += (_, _) => Refresh();
        ActualThemeChanged += (_, _) => Refresh();
    }

    public static string ColorName(Color color) => color switch
    {
        { R: 0xD1, G: 0x34, B: 0x38 } => "red, from the MergedDictionary",
        { R: 0x10, G: 0x7C, B: 0x10 } => "green, from ThemeDictionaries[Light]",
        { R: 0x00, G: 0xB7, B: 0xC3 } => "teal, from ThemeDictionaries[Dark]",
        _ => $"#{color.R:X2}{color.G:X2}{color.B:X2}, probably ThemeDictionaries[HighContrast]",
    };

    private void OnRefresh(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh()
    {
        var color = (Swatch.Background as SolidColorBrush)?.Color ?? default;
        SwatchText.Text = $"ActualTheme = {ActualTheme}. The swatch resolved to {ColorName(color)}.";

        string systemContrast;
        try
        {
            systemContrast = new AccessibilitySettings().HighContrast.ToString();
        }
        catch (Exception ex)
        {
            systemContrast = $"unavailable ({ex.GetType().Name})";
        }

        ContrastText.Text =
            $"AccessibilitySettings.HighContrast = {systemContrast}{Environment.NewLine}" +
            $"Application.RequestedTheme = {Application.Current.RequestedTheme}, this control's ActualTheme = {ActualTheme}";
    }
}
