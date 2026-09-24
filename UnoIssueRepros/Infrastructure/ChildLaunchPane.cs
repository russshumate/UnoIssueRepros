namespace UnoIssueRepros.Infrastructure;

/// <summary>
/// The Repro pane for issues that crash or hang: it runs the repro in a child copy of this
/// app so the launcher survives, and shows what the child printed and how it exited.
/// </summary>
public sealed class ChildLaunchPane : UserControl
{
    private readonly int _issueNumber;
    private readonly ChildProcessRunner _runner = new();
    private readonly LogView _log = new(260);
    private readonly TextBlock _status = Ui.Result("Not run yet.");
    private readonly CheckBox _showAll = new() { Content = "Show all child output (Uno logging is noisy)" };
    private readonly List<(string Line, bool IsError)> _lines = [];

    /// <param name="browserFallback">
    /// Optional content to run in-process where child processes are unavailable (WebAssembly),
    /// for repros that misbehave without taking the whole app down.
    /// </param>
    public ChildLaunchPane(int issueNumber, string explanation, bool hasScriptedRun = true, Func<UIElement>? browserFallback = null)
    {
        _issueNumber = issueNumber;

        var panel = Ui.Stack(Ui.Para(explanation));

        if (!ChildProcessRunner.IsSupported)
        {
            if (browserFallback is not null)
            {
                panel.Children.Add(Ui.Para(
                    "Child processes need the desktop head, so on WebAssembly the repro runs inline below. " +
                    "Watch the browser console for the exceptions."));
                panel.Children.Add(browserFallback());
            }
            else
            {
                panel.Children.Add(Ui.Para(
                    "This repro runs in a child process, which needs the desktop head. " +
                    "Run the net10.0-desktop target to try it."));
            }

            Content = panel;
            return;
        }

        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        if (hasScriptedRun)
        {
            buttons.Children.Add(Ui.Button("Run scripted repro", () => Launch(auto: true)));
        }
        buttons.Children.Add(Ui.Button(hasScriptedRun ? "Open child to try by hand" : "Open child process", () => Launch(auto: false)));
        buttons.Children.Add(Ui.Button("Kill child", () => _runner.Kill()));
        panel.Children.Add(buttons);

        _showAll.Checked += (_, _) => Rerender();
        _showAll.Unchecked += (_, _) => Rerender();
        panel.Children.Add(_status);
        panel.Children.Add(_showAll);
        panel.Children.Add(_log);

        _runner.LineReceived += OnLine;
        _runner.Exited += OnExited;
        Unloaded += (_, _) => _runner.Dispose();

        Content = panel;
    }

    private void Launch(bool auto)
    {
        _lines.Clear();
        _log.Clear();

        var args = new List<string> { ChildMode.ReproArg, _issueNumber.ToString() };
        if (auto)
        {
            args.Add(ChildMode.AutoArg);
        }

        var pid = _runner.Start(args);
        _status.Text = $"Child process {pid} running ({(auto ? "scripted" : "by hand")})…";
        _status.Foreground = Ui.Brush("TextFillColorPrimaryBrush");
    }

    private void OnLine(string line, bool isError)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _lines.Add((line, isError));
            if (_showAll.IsChecked == true || IsInteresting(line))
            {
                _log.Append((isError ? "ERR " : string.Empty) + line);
            }
        });
    }

    private void OnExited(int exitCode)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var crashed = exitCode != 0;
            _status.Text = "Child exited: " + ChildProcessRunner.DescribeExitCode(exitCode);
            _status.Foreground = Ui.Brush(crashed ? "SystemFillColorCriticalBrush" : "SystemFillColorSuccessBrush");
        });
    }

    private void Rerender()
    {
        _log.Clear();
        foreach (var (line, isError) in _lines)
        {
            if (_showAll.IsChecked == true || IsInteresting(line))
            {
                _log.Append((isError ? "ERR " : string.Empty) + line);
            }
        }
    }

    private static bool IsInteresting(string line) =>
        line.Contains(ChildMode.LinePrefix, StringComparison.Ordinal)
        || line.Contains("xception", StringComparison.Ordinal)
        || line.Contains("overflow", StringComparison.OrdinalIgnoreCase)
        || line.Contains("cycle", StringComparison.OrdinalIgnoreCase)
        || line.Contains("fail", StringComparison.OrdinalIgnoreCase);
}
