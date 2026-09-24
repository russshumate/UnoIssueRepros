namespace UnoIssueRepros.Infrastructure;

/// <summary>Window content for a child process: a banner plus the issue's crashing content.</summary>
public sealed class ChildHost : UserControl
{
    public ChildHost(IssueInfo issue)
    {
        var banner = new Border
        {
            Background = Ui.Brush("SystemFillColorCautionBackgroundBrush"),
            Padding = new Thickness(12, 8, 12, 8),
            Child = Ui.Para(
                $"Child process {Environment.ProcessId} for issue {issue.Number}: {issue.Title}. " +
                "This window is expected to crash or hang. The launcher captures its output."),
        };

        var content = issue.CreateChild!();
        var grid = new Grid { RowSpacing = 8 };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(banner);

        var body = new Border { Padding = new Thickness(12), Child = content };
        Grid.SetRow(body, 1);
        grid.Children.Add(body);
        Content = grid;

        Loaded += async (_, _) =>
        {
            ChildMode.Report($"child window loaded (issue {issue.Number}, auto={ChildMode.Auto})");
            ChildMode.StartWatchdog(DispatcherQueue, TimeSpan.FromSeconds(5));
            if (ChildMode.Auto && content is IScriptedRepro scripted)
            {
                await scripted.RunAsync();
                ChildMode.Report("scripted run finished and the process is still alive");
                ChildMode.Exit(0);
            }
        };
    }
}
