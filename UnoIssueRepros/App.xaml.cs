using System.Diagnostics.CodeAnalysis;
using Uno.Extensions.Navigation;
using Uno.Resizetizer;
using UnoIssueRepros.Infrastructure;
using UnoIssueRepros.Pages;

namespace UnoIssueRepros;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    public static Window? MainWindow { get; private set; }

    /// <summary>
    /// A static service provider, set once the host is built. Issue 12's workaround falls back
    /// to it when FrameworkElement.GetServiceProvider() returns null.
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    protected IHost? Host { get; private set; }

    [SuppressMessage("Trimming", "IL2026", Justification = "Uno.Extensions APIs are used in a trimming-safe way here.")]
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ChildMode.Parse(Environment.GetCommandLineArgs());

        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    logBuilder
                        .SetMinimumLevel(context.HostingEnvironment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning)
                        .CoreLogLevel(LogLevel.Warning);
                }, enableUnoLogging: true)
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<Issues.Issue12.GreetingService>();
                }));

        MainWindow = builder.Window;
        MainWindow.SetWindowIcon();
        Host = builder.Build();
        Services = Host.Services;

        if (ChildMode.IsChild)
        {
            LaunchChild(MainWindow);
            return;
        }

        MainWindow.Title = "Uno Issue Repros";
        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindow.Content = rootFrame;
        }

        // Makes FrameworkElement.GetServiceProvider() work for elements under the root frame (issue 12).
        rootFrame.AttachServiceProvider(Host.Services);

        if (rootFrame.Content is null)
        {
            rootFrame.Navigate(typeof(MainPage));
        }

        MainWindow.Activate();
    }

    private void LaunchChild(Window window)
    {
        ChildMode.HookUnhandledExceptions(this);
        ChildMode.Report($"child process {Environment.ProcessId} started, backend={RenderBackend.Current}");

        if (ChildMode.Probe is { } probe)
        {
            window.Title = $"Probe: {probe}";
            window.Content = Probes.Create(probe);
        }
        else if (ChildMode.IssueNumber is { } number && IssueCatalog.Find(number) is { CreateChild: not null } issue)
        {
            window.Title = $"Child repro #{number}";
            var host = new ChildHost(issue);
            host.AttachServiceProvider(Host!.Services);
            window.Content = host;
        }
        else
        {
            ChildMode.Report($"unknown child arguments: {string.Join(' ', Environment.GetCommandLineArgs())}");
            ChildMode.Exit(2);
            return;
        }

        window.Activate();
    }
}
