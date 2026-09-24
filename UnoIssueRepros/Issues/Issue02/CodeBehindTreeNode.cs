using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue02;

/// <summary>
/// The workaround: build the recursive part in code-behind. Each child gets its own node
/// when its DataContext arrives, so the recursion is bounded by the data and stops at leaves.
/// </summary>
public sealed class CodeBehindTreeNode : UserControl
{
    private readonly TextBlock _name = new();
    private readonly StackPanel _children = new() { Margin = new Thickness(16, 0, 0, 0) };

    public CodeBehindTreeNode()
    {
        Content = new StackPanel { Children = { _name, _children } };
        DataContextChanged += (_, _) => Rebuild();
    }

    private void Rebuild()
    {
        _children.Children.Clear();
        if (DataContext is not TreeNode node)
        {
            _name.Text = string.Empty;
            return;
        }

        _name.Text = node.Name;
        foreach (var child in node.Children)
        {
            _children.Children.Add(new CodeBehindTreeNode { DataContext = child });
        }
    }
}

public static class Issue02Panes
{
    public static UIElement Workaround() => Ui.Stack(
        Ui.Para(
            "Replace the self-referencing ItemsRepeater with a plain StackPanel and add child nodes " +
            "from DataContextChanged. Setting each child's DataContext builds the grandchildren, " +
            "so construction stops at the leaves."),
        Ui.Code(
            "DataContextChanged += (_, _) =>\n" +
            "{\n" +
            "    _children.Children.Clear();\n" +
            "    foreach (var child in ((TreeNode)DataContext).Children)\n" +
            "        _children.Children.Add(new CodeBehindTreeNode { DataContext = child });\n" +
            "};"),
        Ui.Heading("Live"),
        new CodeBehindTreeNode { DataContext = TreeNode.Sample },
        Ui.Para("Non-recursive ItemTemplates are fine. The eager build only hurts when a template contains its own owner."));
}
