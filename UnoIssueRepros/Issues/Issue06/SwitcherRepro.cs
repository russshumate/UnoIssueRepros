using UnoIssueRepros.Infrastructure;
using UnoIssueRepros.Issues.Shared;
using Windows.Graphics;

// Uno0001 ("not implemented in Uno") is expected: calling IsShownInSwitchers is the repro.
#pragma warning disable Uno0001

namespace UnoIssueRepros.Issues.Issue06;

/// <summary>Sets AppWindow.IsShownInSwitchers = false on a second window and parks it off-screen.</summary>
public sealed class SwitcherRepro : UserControl
{
    private const string ProbeTitle = "Issue 6 probe window";

    private readonly TextBlock _result = Ui.Result();
    private Window? _probe;

    public SwitcherRepro()
    {
        if (!WindowTools.MultiWindowSupported)
        {
            Content = WindowTools.DesktopOnlyNotice();
            return;
        }

        Content = Ui.Stack(
            Ui.Para("Expected: with IsShownInSwitchers = false the window has no taskbar button and is not in Alt+Tab. " +
                    "On Uno Skia Win32 the property is not implemented, so a window parked off-screen (because Hide() " +
                    "does nothing, see issue 3) still shows in the taskbar and Alt+Tab as a ghost."),
            Ui.Row(
                Ui.Button("Open probe, IsShownInSwitchers = false", OpenAsync),
                Ui.Button("Park it off-screen", Park),
                Ui.Button("Close probe", CloseProbe)),
            Ui.Para($"Then look at the taskbar and press Alt+Tab. If \"{ProbeTitle}\" is listed, the issue reproduced."),
            _result);
        Unloaded += (_, _) => CloseProbe();
    }

    private async Task OpenAsync()
    {
        CloseProbe();
        _probe = WindowTools.CreateProbeWindow(ProbeTitle, "IsShownInSwitchers was set to false on this window.");
        _probe.Closed += (_, _) => _probe = null;

        Exception? error = null;
        try
        {
            _probe.AppWindow.IsShownInSwitchers = false;
        }
        catch (Exception ex)
        {
            error = ex;
        }

        _probe.Activate();
        await WindowTools.Settle();
        Describe(error);
    }

    private void Park()
    {
        if (_probe is null)
        {
            _result.Text = "Open the probe first.";
            return;
        }

        _probe.AppWindow.Move(new PointInt32 { X = -32000, Y = -32000 });
        Describe(null);
    }

    private void Describe(Exception? error)
    {
        var native = Win32.FindByTitle(ProbeTitle);
        _result.Text =
            $"IsShownInSwitchers read back as {_probe?.AppWindow.IsShownInSwitchers}{Environment.NewLine}" +
            (error is null ? string.Empty : $"Setting it threw {error.GetType().Name}: {error.Message}{Environment.NewLine}") +
            (native is null
                ? "Win32 window not found."
                : $"Win32 extended style 0x{native.ExStyle:X}: WS_EX_TOOLWINDOW={native.IsToolWindow}, WS_EX_APPWINDOW={native.IsAppWindow}" +
                  (native.IsToolWindow ? "" : " (a normal app window, so it gets a taskbar button)"));
    }

    private void CloseProbe()
    {
        _probe?.Close();
        _probe = null;
    }
}
