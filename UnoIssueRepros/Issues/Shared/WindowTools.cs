using System.Diagnostics;
using UnoIssueRepros.Infrastructure;
using Windows.Graphics;

namespace UnoIssueRepros.Issues.Shared;

public static class WindowTools
{
    public static bool MultiWindowSupported => !OperatingSystem.IsBrowser();

    public static UIElement DesktopOnlyNotice() =>
        Ui.Para("This repro opens extra windows, so it needs the desktop head. WebAssembly has a single window.");

    public static Window CreateProbeWindow(string title, string body)
    {
        var window = new Window
        {
            Title = title,
            Content = new Border { Padding = new Thickness(16), Child = Ui.Para(body) },
        };
        window.AppWindow.Resize(new SizeInt32 { Width = 460, Height = 220 });
        return window;
    }

    /// <summary>Gives a freshly activated window a moment to be created and shown.</summary>
    public static Task Settle() => Task.Delay(400);
}

/// <summary>
/// The workaround shared by issues 3 and 6: instead of hiding or parking a window, close it
/// (cheap) and create a new one at the same position when it is needed again.
/// </summary>
public sealed class CloseAndRecreatePane : UserControl
{
    private readonly TextBlock _result = Ui.Result();
    private Window? _window;
    private PointInt32? _lastPosition;

    public CloseAndRecreatePane(string intro)
    {
        if (!WindowTools.MultiWindowSupported)
        {
            Content = WindowTools.DesktopOnlyNotice();
            return;
        }

        Content = Ui.Stack(
            Ui.Para(intro),
            Ui.Row(
                Ui.Button("Show window", Show),
                Ui.Button("\"Hide\" it (close, remember position)", Hide)),
            _result);
        Unloaded += (_, _) => _window?.Close();
    }

    private void Show()
    {
        if (_window is not null)
        {
            _result.Text = "The window is already showing.";
            return;
        }

        var timer = Stopwatch.StartNew();
        _window = WindowTools.CreateProbeWindow("Close-and-recreate window", "Created fresh. \"Hide\" closes it for real.");
        var created = timer.ElapsedMilliseconds;
        if (_lastPosition is { } position)
        {
            _window.AppWindow.Move(position);
        }
        _window.Closed += (_, _) => _window = null;
        _window.Activate();
        _result.Text = $"new Window() took {created} ms, shown in {timer.ElapsedMilliseconds} ms" +
                       (_lastPosition is null ? "." : $", restored to {_lastPosition.Value.X},{_lastPosition.Value.Y}.");
    }

    private void Hide()
    {
        if (_window is null)
        {
            _result.Text = "No window to hide.";
            return;
        }

        var timer = Stopwatch.StartNew();
        _lastPosition = _window.AppWindow.Position;
        _window.Close();
        _window = null;
        _result.Text = $"Closed in {timer.ElapsedMilliseconds} ms. Nothing is left in the taskbar or Alt+Tab.";
    }
}
