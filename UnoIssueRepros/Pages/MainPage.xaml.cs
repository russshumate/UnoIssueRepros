using Microsoft.UI.Text;
using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Pages;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        EnvironmentText.Text = EnvironmentInfo.Describe();
        BuildList();
    }

    private void BuildList()
    {
        foreach (var group in IssueCatalog.All.GroupBy(i => i.Group))
        {
            GroupsPanel.Children.Add(Ui.Heading(IssueInfo.GroupTitle(group.Key)));

            foreach (var issue in group)
            {
                GroupsPanel.Children.Add(CreateRow(issue));
            }
        }
    }

    private UIElement CreateRow(IssueInfo issue)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var number = new TextBlock
        {
            Text = issue.Number.ToString(),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var title = new TextBlock
        {
            Text = issue.Title,
            TextWrapping = TextWrapping.WrapWholeWords,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var mode = new Border
        {
            Background = Ui.Brush("SubtleFillColorSecondaryBrush"),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = issue.ModeLabel, FontSize = 12 },
        };
        Grid.SetColumn(title, 1);
        Grid.SetColumn(mode, 2);
        grid.Children.Add(number);
        grid.Children.Add(title);
        grid.Children.Add(mode);

        var button = new Button
        {
            Content = grid,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12, 8, 12, 8),
        };
        AutomationProperties.SetName(button, $"Issue {issue.Number}: {issue.Title}");
        button.Click += (_, _) => Frame.Navigate(typeof(IssuePage), issue.Number);
        return button;
    }
}
