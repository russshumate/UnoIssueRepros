using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue04;

/// <summary>Compares AppWindow.Id with the real HWND of the same window.</summary>
public sealed class WindowIdRepro : UserControl
{
    private readonly TextBlock _result = Ui.Result();
    private readonly TextBlock _verdict = Ui.Result();

    public WindowIdRepro()
    {
        if (!Win32.IsAvailable)
        {
            Content = Ui.Para("This repro compares against Win32 window handles, so it needs the desktop head on Windows.");
            return;
        }

        Content = Ui.Stack(
            Ui.Para("On WinUI, AppWindow.Id.Value is the window's HWND, so interop code can pass it to Win32. " +
                    "On Uno Skia Win32 it is not: Win32 does not recognise it as a window."),
            Ui.Button("Compare", Run),
            _verdict,
            _result);
        Loaded += (_, _) => Run();
    }

    private void Run()
    {
        var window = App.MainWindow!;
        var id = window.AppWindow.Id.Value;
        var windows = Win32.WindowsOfThisProcess();
        var byTitle = Win32.FindByTitle(window.Title);

        _result.Text =
            $"AppWindow.Id.Value     = 0x{id:X} ({id}){Environment.NewLine}" +
            $"Win32 IsWindow(Id)     = {Win32.IsRealWindow((nint)id)}{Environment.NewLine}" +
            $"HWND found by title    = {(byTitle is null ? "(none)" : $"0x{byTitle.Handle:X}")}{Environment.NewLine}" +
            $"Top-level windows of this process:{Environment.NewLine}  " +
            string.Join(Environment.NewLine + "  ", windows.Select(w => w.ToString()));

        var mismatch = byTitle is null || (ulong)byTitle.Handle != id;
        Ui.SetVerdict(_verdict, mismatch, mismatch
            ? "AppWindow.Id is not this window's HWND."
            : "AppWindow.Id matches the HWND.");
    }
}

public sealed class WindowIdWorkaround : UserControl
{
    private readonly TextBlock _result = Ui.Result();

    public WindowIdWorkaround()
    {
        if (!Win32.IsAvailable)
        {
            Content = Ui.Para("Needs the desktop head on Windows.");
            return;
        }

        Content = Ui.Stack(
            Ui.Para("Enumerate this process's top-level windows and pick the one with Uno's window class " +
                    $"({Win32.UnoWindowClass}) and the window's title. Give windows unique titles, or set a " +
                    "temporary unique title while you look the handle up."),
            Ui.Code(
                "EnumWindows((hwnd, _) =>\n" +
                "{\n" +
                "    GetWindowThreadProcessId(hwnd, out var pid);\n" +
                "    if (pid == Environment.ProcessId\n" +
                $"        && ClassName(hwnd) == \"{Win32.UnoWindowClass}\"\n" +
                "        && Title(hwnd) == window.Title) found = hwnd;\n" +
                "    return true;\n" +
                "}, 0);"),
            Ui.Para("An alternative that also worked for drag handling: GetForegroundWindow() at the moment the " +
                    "user starts the gesture, checked against this process id."),
            Ui.Button("Find the main window's HWND", Run),
            _result);
    }

    private void Run()
    {
        var title = App.MainWindow!.Title;
        var found = Win32.FindByTitle(title);
        _result.Text = found is null
            ? "Not found."
            : $"HWND 0x{found.Handle:X}, IsWindow = {Win32.IsRealWindow(found.Handle)}{Environment.NewLine}{found}";
    }
}
