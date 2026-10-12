using Microsoft.UI.Xaml.Media;
using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue23;

/// <summary>
/// Flips a subtree's RequestedTheme to the opposite of the application theme (the in-app theme
/// toggle the docs describe), gives a Style-themed element a local Foreground, then clears it.
/// The Style's {ThemeResource} should come back resolved for the element's theme; it comes back
/// resolved for the application's.
/// </summary>
public sealed partial class ClearValueThemeRepro : UserControl
{
    public ClearValueThemeRepro()
    {
        this.InitializeComponent();
        IntroText.Text =
            "The host's Foreground is a Style {ThemeResource TextFillColorPrimaryBrush}. This subtree's RequestedTheme is " +
            "flipped to the opposite of Application.RequestedTheme, the host gets a local red Foreground, and the local " +
            "value is then removed with ClearValue(ForegroundProperty). The brush before the local set and the brush after " +
            "ClearValue should be the same (the element's theme did not change).";
        Loaded += async (_, _) =>
        {
            await Task.Delay(500);
            await RunAsync();
        };
    }

    private async void OnRun(object sender, RoutedEventArgs e) => await RunAsync();

    internal async Task RunAsync()
    {
        var appTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var target = appTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;

        Root.RequestedTheme = appTheme;
        await Task.Delay(400);
        Root.RequestedTheme = target;
        await Task.Delay(700); // ActualThemeChanged is posted; let the walk settle
        var afterSwitch = Describe(Host.Foreground);
        var themeAfterSwitch = Host.ActualTheme;

        Host.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
        await Task.Delay(400);
        Host.ClearValue(Control.ForegroundProperty);
        await Task.Delay(700);
        var afterClear = Describe(Host.Foreground);

        ResultText.Text =
            $"Application theme: {appTheme}; subtree RequestedTheme: {target}; Host.ActualTheme: {themeAfterSwitch} -> {Host.ActualTheme}{Environment.NewLine}" +
            $"Host.Foreground after the switch:  {afterSwitch}{Environment.NewLine}" +
            $"Host.Foreground after ClearValue:  {afterClear}";

        var reproduced = afterSwitch != afterClear && Host.ActualTheme == themeAfterSwitch;
        Ui.SetVerdict(VerdictText, reproduced, reproduced
            ? $"ClearValue re-resolved the Style's ThemeResource to {afterClear} (the application theme's value) although the element is still {Host.ActualTheme}."
            : "ClearValue restored the brush the element had for its own theme.");
    }

    internal static string Describe(Brush? brush) => brush is SolidColorBrush s ? s.Color.ToString() : brush?.GetType().Name ?? "(null)";
}
