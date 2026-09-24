using Microsoft.UI.Xaml.Navigation;
using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Pages;

public sealed partial class IssuePage : Page
{
    private IssueInfo? _issue;

    public IssuePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Show(e.Parameter is int number ? IssueCatalog.Find(number) : null);
    }

    private void Show(IssueInfo? issue)
    {
        _issue = issue;
        if (issue is null)
        {
            TitleText.Text = "Unknown issue";
            return;
        }

        GroupText.Text = $"#{issue.Number} · {IssueInfo.GroupTitle(issue.Group)} · {issue.ModeLabel}";
        TitleText.Text = issue.Title;
        SummaryText.Text = issue.Summary;
        MetaText.Text = $"Ask: {issue.Ask}{Environment.NewLine}Applies to: {issue.Platforms}{Environment.NewLine}{EnvironmentInfo.Describe()}";

        ReproHost.Content = issue.CreateRepro();
        WorkaroundHost.Content = issue.CreateWorkaround();

        var index = IssueCatalog.IndexOf(issue);
        PreviousButton.IsEnabled = index > 0;
        NextButton.IsEnabled = index < IssueCatalog.All.Count - 1;
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(typeof(MainPage));
        }
    }

    private void OnPrevious(object sender, RoutedEventArgs e) => Step(-1);

    private void OnNext(object sender, RoutedEventArgs e) => Step(+1);

    private void Step(int delta)
    {
        if (_issue is null)
        {
            return;
        }

        var index = IssueCatalog.IndexOf(_issue) + delta;
        if (index >= 0 && index < IssueCatalog.All.Count)
        {
            // Swap in place rather than stacking pages, so Back still returns to the list.
            Show(IssueCatalog.All[index]);
        }
    }
}
