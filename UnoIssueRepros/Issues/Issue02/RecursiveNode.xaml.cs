using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue02;

public sealed partial class RecursiveNode : UserControl
{
    private static int _constructed;

    public RecursiveNode()
    {
        var count = Interlocked.Increment(ref _constructed);
        if (count is 1 or 2 or 5 or 10 or 100 or 1_000 or 5_000 or 10_000)
        {
            ChildMode.Report($"RecursiveNode constructor entered {count} time(s)");
        }

        this.InitializeComponent();
    }

    public static int Constructed => _constructed;
}

/// <summary>Child window content: creates a RecursiveNode for a three-level tree.</summary>
public sealed class RecursiveRepro : UserControl, IScriptedRepro
{
    private readonly StackPanel _panel = new() { Spacing = 8 };

    public RecursiveRepro()
    {
        _panel.Children.Add(Ui.Para(
            "Press the button to construct one RecursiveNode bound to a three-level tree. " +
            "Expected: the tree renders. On Uno the process dies with a stack overflow."));
        _panel.Children.Add(Ui.Button("Create the recursive control", () => { _ = RunAsync(); }));
        Content = _panel;
    }

    public Task RunAsync()
    {
        ChildMode.Report("constructing RecursiveNode (the stack overflow, if any, happens here)");
        var node = new RecursiveNode { DataContext = TreeNode.Sample };
        _panel.Children.Add(node);
        ChildMode.Report($"RESULT not reproduced: constructed without overflow, {RecursiveNode.Constructed} RecursiveNode instance(s)");
        return Task.Delay(1000);
    }
}
