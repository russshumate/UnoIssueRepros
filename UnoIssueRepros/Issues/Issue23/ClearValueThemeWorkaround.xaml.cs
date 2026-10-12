using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue23;

/// <summary>
/// Same subtree flip, but the conditional look is a second Style. Swapping Styles never leaves a
/// local value to clear, and a Style's {ThemeResource} is applied against the element's own theme.
/// </summary>
public sealed partial class ClearValueThemeWorkaround : UserControl
{
    public ClearValueThemeWorkaround()
    {
        this.InitializeComponent();
        IntroText.Text =
            "Never ClearValue a theme-bound Foreground inside a RequestedTheme subtree. Put the conditional look in a second " +
            "Style and swap Styles, or set the wanted brush explicitly (resolved for the element's theme). Here the host goes " +
            "ThemedHost -> HighlightedHost -> ThemedHost.";
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
        var themed = (Style)Root.Resources["ThemedHost"];
        var highlighted = (Style)Root.Resources["HighlightedHost"];

        Root.RequestedTheme = appTheme;
        await Task.Delay(400);
        Root.RequestedTheme = target;
        await Task.Delay(700);
        var afterSwitch = ClearValueThemeRepro.Describe(Host.Foreground);

        Host.Style = highlighted;
        await Task.Delay(400);
        Host.Style = themed;
        await Task.Delay(700);
        var afterSwap = ClearValueThemeRepro.Describe(Host.Foreground);

        ResultText.Text =
            $"Application theme: {appTheme}; subtree RequestedTheme: {target}; Host.ActualTheme: {Host.ActualTheme}{Environment.NewLine}" +
            $"Host.Foreground after the switch:  {afterSwitch}{Environment.NewLine}" +
            $"Host.Foreground after the Style swap back: {afterSwap}";

        var fixedIt = afterSwitch == afterSwap;
        Ui.SetVerdict(VerdictText, !fixedIt, fixedIt
            ? "swapping Styles brought back the brush for the element's own theme."
            : $"the Style swap came back with {afterSwap}, not {afterSwitch}.");
    }
}
