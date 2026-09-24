using System.Diagnostics;
using System.Text.RegularExpressions;
using UnoIssueRepros.Infrastructure;
using UnoIssueRepros.Issues.Shared;

namespace UnoIssueRepros.Issues.Issue20;

/// <summary>
/// Child probe (<c>--probe window-timing</c>): times new Window() a few times on whatever
/// render backend the process was started with, prints the numbers, and exits.
/// </summary>
public sealed class WindowTimingProbe : UserControl
{
    public const string ProbeName = "window-timing";
    private const int Samples = 3;

    public WindowTimingProbe()
    {
        Content = Ui.Para($"Timing new Window() on backend {RenderBackend.Current}…");
        Loaded += async (_, _) =>
        {
            ChildMode.StartWatchdog(DispatcherQueue, TimeSpan.FromSeconds(30));
            await Task.Delay(500);
            var timings = new List<long>();
            for (var i = 0; i < Samples; i++)
            {
                var timer = Stopwatch.StartNew();
                var window = new Window { Title = $"timing {i + 1}" };
                timings.Add(timer.ElapsedMilliseconds);
                window.Close();
                await Task.Delay(100);
            }

            ChildMode.Report($"RESULT backend={RenderBackend.Current} newWindowMs={string.Join(",", timings)}");
            ChildMode.Exit(0);
        };
    }
}

/// <summary>Launches the timing probe once per backend and tabulates the results.</summary>
public sealed class WindowTimingPane : UserControl
{
    private static readonly Regex ResultLine = new(@"RESULT backend=(?<backend>\S+) newWindowMs=(?<ms>[\d,]+)");

    private readonly TextBlock _table = Ui.Result();
    private readonly LogView _log = new(120);

    public WindowTimingPane()
    {
        if (!ChildProcessRunner.IsSupported || !WindowTools.MultiWindowSupported)
        {
            Content = Ui.Para("Needs the desktop head: each backend runs in its own child process.");
            return;
        }

        Content = Ui.Stack(
            Ui.Para("Solved, and not an Uno bug. A multi-second freeze on every new Window() turned out to be a stale " +
                    "Intel Arc GPU driver. OpenGL context creation blocked the UI thread for about 3.6 s. On the bad driver: " +
                    "OpenGL 3608 ms, software 17 ms, Vulkan 716 ms. After a driver update, new Window() took about 12 ms."),
            Ui.Para("Run the same measurement here. Each backend is a separate child process, because the backend is fixed at startup."),
            Ui.Button("Time new Window() on each backend", RunAllAsync),
            _table,
            _log);
    }

    private async Task RunAllAsync()
    {
        var rows = new List<string>();
        _table.Text = "Running…";
        foreach (var backend in RenderBackend.Choices)
        {
            var result = await RunOneAsync(backend);
            rows.Add($"{backend,-9} {result}");
            _table.Text = string.Join(Environment.NewLine, rows);
        }
    }

    private Task<string> RunOneAsync(string backend)
    {
        var completion = new TaskCompletionSource<string>();
        string? result = null;
        var runner = new ChildProcessRunner();
        runner.LineReceived += (line, _) =>
        {
            if (line.Contains(ChildMode.LinePrefix, StringComparison.Ordinal))
            {
                _log.Append($"[{backend}] {line}");
            }

            var match = ResultLine.Match(line);
            if (match.Success)
            {
                result = $"new Window(): {match.Groups["ms"].Value.Replace(",", " ms, ")} ms";
            }
        };
        runner.Exited += code =>
        {
            completion.TrySetResult(result ?? ChildProcessRunner.DescribeExitCode(code));
            runner.Dispose();
        };
        runner.Start(
            [ChildMode.ProbeArg, WindowTimingProbe.ProbeName],
            new Dictionary<string, string> { [RenderBackend.EnvironmentVariable] = backend });
        return completion.Task;
    }
}

public static class Issue20Panes
{
    public static UIElement Lesson() => new StepsView("""
        The lesson: bisect a multi-second UI freeze by render backend before blaming the framework.
        ## Signs it is a block, not work
        - Wall-clock time is high but CPU time is low (3608 ms wall, 406 ms CPU here). Something is waiting on a driver or another process.
        - A blank app shows the same delay. That only rules out your code, not the framework.
        ## Cheap checks, in order
        - Switch the render backend. A spread this large across backends points to the GPU driver.
        $ Uno.UI.FeatureConfiguration.Rendering.UseOpenGLOnWin32 = false;   // software
        $ Uno.UI.FeatureConfiguration.Rendering.UseVulkanOnWin32 = true;    // Vulkan
        - Set the flags before the host is built. This app reads UNOREPRO_RENDER_BACKEND in Platforms/Desktop/Program.cs.
        - Compare Debug and Release, to rule out Hot Reload and DevServer instrumentation.
        - Check how old the driver is. Two GPUs with driver dates ten months apart was the smell here.
        $ Get-CimInstance Win32_VideoController | Select-Object Name, DriverVersion, DriverDate
        ## Insurance we kept
        A small pool of pre-created windows, filled when the user is idle, so a slow driver on a customer's machine cannot freeze a click.
        """);
}
