namespace UnoIssueRepros.Infrastructure;

/// <summary>
/// A child process is this same app launched with <c>--repro N</c> (optionally <c>--auto</c>)
/// or <c>--probe name</c>. It shows only the crashing content and writes "[repro]" lines
/// to stdout, which the parent's <see cref="ChildLaunchPane"/> captures.
/// </summary>
public static class ChildMode
{
    public const string ReproArg = "--repro";
    public const string AutoArg = "--auto";
    public const string ProbeArg = "--probe";
    public const string LinePrefix = "[repro]";

    public static int? IssueNumber { get; private set; }

    public static string? Probe { get; private set; }

    /// <summary>Run the scripted scenario, then exit. Without it the child stays open for manual poking.</summary>
    public static bool Auto { get; private set; }

    public static bool IsChild => IssueNumber is not null || Probe is not null;

    public static void Parse(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case ReproArg when i + 1 < args.Length && int.TryParse(args[i + 1], out var number):
                    IssueNumber = number;
                    i++;
                    break;
                case ProbeArg when i + 1 < args.Length:
                    Probe = args[++i];
                    break;
                case AutoArg:
                    Auto = true;
                    break;
            }
        }
    }

    /// <summary>Writes a line the parent will always show, and flushes so it survives a hard crash.</summary>
    public static void Report(string message)
    {
        Console.Out.WriteLine($"{LinePrefix} {message}");
        Console.Out.Flush();
    }

    public static void Exit(int code)
    {
        Report($"exiting with code {code}");
        Environment.Exit(code);
    }

    public const int HungExitCode = 3;

    /// <summary>
    /// Exits with <see cref="HungExitCode"/> if the UI thread stops responding (an endless layout
    /// pass, for example), so a hang is reported rather than left running.
    /// </summary>
    public static void StartWatchdog(DispatcherQueue dispatcher, TimeSpan limit)
    {
        var lastBeat = Environment.TickCount64;
        var timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(200);
        timer.Tick += (_, _) => Interlocked.Exchange(ref lastBeat, Environment.TickCount64);
        timer.Start();

        var thread = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(500);
                var silent = Environment.TickCount64 - Interlocked.Read(ref lastBeat);
                if (silent > limit.TotalMilliseconds)
                {
                    Report($"RESULT reproduced: the UI thread has not responded for {silent} ms (hung)");
                    Exit(HungExitCode);
                }
            }
        })
        { IsBackground = true, Name = "repro watchdog" };
        thread.Start();
    }

    /// <summary>Reports every unhandled exception path so the parent sees why the child died.</summary>
    public static void HookUnhandledExceptions(Application app)
    {
        app.UnhandledException += (_, e) =>
            Report($"Application.UnhandledException: {e.Exception.GetType().FullName}: {e.Exception.Message}{Environment.NewLine}{e.Exception.StackTrace}");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Report($"AppDomain.UnhandledException (terminating={e.IsTerminating}): {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
            Report($"TaskScheduler.UnobservedTaskException: {e.Exception}");

        // Exceptions that the framework catches and swallows never reach the handlers above.
        // First-chance notifications show them anyway (the first few, with the throwing frame).
        var firstChance = 0;
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            if (e.Exception is OperationCanceledException || Interlocked.Increment(ref firstChance) > 25)
            {
                return;
            }

            var thrownAt = e.Exception.StackTrace?.Split('\n', 2)[0].Trim() ?? "(no stack)";
            Report($"first-chance {e.Exception.GetType().Name}: {e.Exception.Message} ({thrownAt})");
        };
    }
}

/// <summary>Child content that can drive its own repro when launched with <c>--auto</c>.</summary>
public interface IScriptedRepro
{
    /// <summary>Runs the scenario. Returning means the process survived it.</summary>
    Task RunAsync();
}
