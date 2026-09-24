using Microsoft.UI.Xaml.Media;
using UnoIssueRepros.Issues.Issue13;
using UnoIssueRepros.Issues.Issue15;
using UnoIssueRepros.Pages;

namespace UnoIssueRepros.Infrastructure;

/// <summary>
/// <c>--probe smoke</c>: opens every issue page in turn, prints the verdict lines that pages
/// compute on load, runs the headless MVUX probes, and exits with the number of failures.
/// Used to check the app end to end without clicking through it.
/// </summary>
public sealed class SmokeProbe : UserControl
{
    public const string ProbeName = "smoke";

    private readonly Frame _frame = new();

    public SmokeProbe()
    {
        Content = _frame;
        Loaded += async (_, _) => ChildMode.Exit(await RunAsync());
    }

    private async Task<int> RunAsync()
    {
        var failures = 0;
        _frame.AttachServiceProviderIfAvailable();
        _frame.Navigate(typeof(MainPage));
        await Task.Delay(500);
        ChildMode.Report($"launch page lists {IssueCatalog.All.Count} issues");

        foreach (var issue in IssueCatalog.All)
        {
            try
            {
                _frame.Navigate(typeof(IssuePage), issue.Number);
                await Task.Delay(issue.Number is 7 or 12 or 22 ? 1600 : 700);
                var verdicts = TextsUnder(_frame).Where(t => t.StartsWith("REPRODUCED", StringComparison.Ordinal)
                    || t.StartsWith("Not reproduced", StringComparison.Ordinal)
                    || t.Contains("ServiceProvider() returned", StringComparison.Ordinal)
                    || t.Contains("swatch resolved", StringComparison.Ordinal)).ToList();
                ChildMode.Report($"#{issue.Number} page OK" + (verdicts.Count == 0 ? string.Empty : ": " + string.Join(" | ", verdicts)));
            }
            catch (Exception ex)
            {
                failures++;
                ChildMode.Report($"#{issue.Number} page FAILED: {ex}");
            }
        }

        failures += await ProbeAsync("#3 Hide()", async () =>
        {
            var repro = new Issues.Issue03.HideRepro();
            _frame.Content = repro;
            await Task.Delay(300);
            var stillVisible = await repro.RunAsync();
            repro.CloseProbe();
            return stillVisible ? "REPRODUCED: still visible after AppWindow.Hide()" : "Not reproduced: the window hid";
        });
        failures += await ProbeAsync("#9 Storyboard", async () =>
        {
            var repro = new Issues.Issue09.StoryboardHold(stopBeforeRestore: false);
            _frame.Content = repro;
            await Task.Delay(300);
            var held = await repro.RunAsync();
            var fix = new Issues.Issue09.StoryboardHold(stopBeforeRestore: true);
            _frame.Content = fix;
            await Task.Delay(300);
            var fixHeld = await fix.RunAsync();
            return $"repro: {(held ? "REPRODUCED (box stays invisible)" : "Not reproduced")} [{repro.Readout}]; workaround: {(fixHeld ? "still invisible" : "box visible again")} [{fix.Readout}]";
        });
        failures += await ProbeAsync("#14 command thread", async () =>
        {
            var viewModel = new Issues.Issue14.Issue14ViewModel();
            ((System.Windows.Input.ICommand)viewModel.Probe).Execute(null);
            await Task.Delay(1500);
            var workaround = new Issues.Issue14.Issue14WorkaroundViewModel(DispatcherQueue);
            ((System.Windows.Input.ICommand)workaround.Probe).Execute(null);
            await Task.Delay(1500);
            return $"UI thread is {Environment.CurrentManagedThreadId}{Environment.NewLine}repro: {viewModel.Report}{Environment.NewLine}workaround: {workaround.Report}";
        });
        failures += await ProbeAsync("#21 move between windows", async () =>
        {
            var demo = new Issues.Issue21.MoveBetweenWindows();
            _frame.Content = demo;
            await Task.Delay(300);
            demo.Count();
            demo.MoveOut();
            await Task.Delay(500);
            var away = !demo.IsHome;
            demo.Count();
            demo.MoveBack();
            await Task.Delay(300);
            return away && demo.IsHome && demo.Counter == 2
                ? "Not reproduced (works): moved out, counted in the other window, came back with counter 2"
                : $"REPRODUCED: away={away} home={demo.IsHome} counter={demo.Counter}";
        });
        failures += await ProbeAsync("#2 through ChildProcessRunner", async () =>
        {
            var exited = new TaskCompletionSource<int>();
            var lines = new List<string>();
            using var runner = new ChildProcessRunner();
            runner.LineReceived += (line, _) => { lock (lines) { lines.Add(line); } };
            runner.Exited += code => exited.TrySetResult(code);
            runner.Start([ChildMode.ReproArg, "2", ChildMode.AutoArg]);
            var finished = await Task.WhenAny(exited.Task, Task.Delay(TimeSpan.FromSeconds(60)));
            if (finished != exited.Task)
            {
                runner.Kill();
                return "child did not exit within 60 s";
            }

            int count;
            lock (lines) { count = lines.Count; }
            return $"{ChildProcessRunner.DescribeExitCode(exited.Task.Result)} ({count} output lines captured)";
        });
        failures += await ProbeAsync("#12 feed", async () =>
        {
            var owner = new object();
            var context = Uno.Extensions.Reactive.Core.SourceContext.GetOrCreate(owner);
            var model = new Issues.Issue12.Issue12Model();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var seen = new List<string>();
            try
            {
                await foreach (var message in model.Message.GetSource(context, cts.Token))
                {
                    seen.Add($"data={(message.Current.Data.IsSome(out var v) ? v.Text : "(none)")} error={message.Current.Error?.ToString() ?? "(none)"}");
                    if (message.Current.Data.IsSome(out _) || message.Current.Error is not null)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                seen.Add("timed out");
            }

            return string.Join(Environment.NewLine, seen);
        });
        failures += await ProbeAsync("#13 repro", async () => (await ConcurrentSubscribers.RunReproAsync()).Report);
        failures += await ProbeAsync("#13 workaround", ConcurrentSubscribers.RunWorkaroundsAsync);
        failures += await ProbeAsync("#15 repro", async () => (await SelectionRefetch.RunAntiPatternAsync()).Report);
        failures += await ProbeAsync("#15 workaround", async () => (await SelectionRefetch.RunPatternAsync()).Report);

        ChildMode.Report($"RESULT smoke finished with {failures} failure(s)");
        return failures;
    }

    private static async Task<int> ProbeAsync(string name, Func<Task<string>> run)
    {
        try
        {
            var report = await run();
            ChildMode.Report($"{name}:{Environment.NewLine}{report}");
            return 0;
        }
        catch (Exception ex)
        {
            ChildMode.Report($"{name} FAILED: {ex}");
            return 1;
        }
    }

    private static IEnumerable<string> TextsUnder(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock { Text: { Length: > 0 } text })
            {
                yield return text;
            }

            foreach (var nested in TextsUnder(child))
            {
                yield return nested;
            }
        }
    }
}

internal static class FrameServiceExtensions
{
    public static void AttachServiceProviderIfAvailable(this Frame frame)
    {
        if (App.Services is { } services)
        {
            Uno.Extensions.Navigation.FrameworkElementExtensions.AttachServiceProvider(frame, services);
        }
    }
}
