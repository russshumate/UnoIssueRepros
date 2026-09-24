using UnoIssueRepros.Infrastructure;
using UnoIssueRepros.Issues.Shared;

// Uno0001 ("not implemented in Uno") is expected: calling Hide() is the repro.
#pragma warning disable Uno0001

namespace UnoIssueRepros.Issues.Issue03;

/// <summary>Opens a second window, calls AppWindow.Hide(), then checks whether it really hid.</summary>
public sealed class HideRepro : UserControl
{
    private const string ProbeTitle = "Issue 3 probe window";

    private readonly TextBlock _result = Ui.Result();
    private readonly TextBlock _verdict = Ui.Result();
    private Window? _probe;

    public HideRepro()
    {
        if (!WindowTools.MultiWindowSupported)
        {
            Content = WindowTools.DesktopOnlyNotice();
            return;
        }

        Content = Ui.Stack(
            Ui.Para("Expected: after Hide() the probe window disappears and AppWindow.IsVisible is false. " +
                    "On Uno Skia Win32 Hide() returns normally, the window stays on screen and IsVisible stays true."),
            Ui.Row(
                Ui.Button("Run: open, then Hide()", async () => { await RunAsync(); }),
                Ui.Button("Close probe", CloseProbe)),
            _verdict,
            _result);
        Unloaded += (_, _) => CloseProbe();
    }

    /// <summary>Runs the sequence; returns true when the window is still visible after Hide().</summary>
    internal async Task<bool> RunAsync()
    {
        CloseProbe();
        _probe = WindowTools.CreateProbeWindow(ProbeTitle, "If you can still see this window after Hide(), the issue reproduced.");
        _probe.Closed += (_, _) => _probe = null;
        _probe.Activate();
        await WindowTools.Settle();

        var before = Snapshot();
        Exception? error = null;
        try
        {
            _probe.AppWindow.Hide();
        }
        catch (Exception ex)
        {
            error = ex;
        }

        await WindowTools.Settle();
        var after = Snapshot();

        _result.Text =
            $"before Hide(): {before}{Environment.NewLine}" +
            $"after Hide():  {after}{Environment.NewLine}" +
            (error is null ? "Hide() returned normally." : $"Hide() threw {error.GetType().Name}: {error.Message}");

        var stillVisible = _probe?.AppWindow.IsVisible == true || Win32.FindByTitle(ProbeTitle)?.Visible == true;
        Ui.SetVerdict(_verdict, stillVisible, stillVisible
            ? "the window is still visible after AppWindow.Hide()."
            : "the window hid.");
        return stillVisible;
    }

    private string Snapshot()
    {
        var isVisible = _probe?.AppWindow.IsVisible.ToString() ?? "(closed)";
        var native = Win32.IsAvailable
            ? Win32.FindByTitle(ProbeTitle) is { } w ? $"Win32 IsWindowVisible={w.Visible}" : "Win32 window not found"
            : "Win32 n/a";
        return $"AppWindow.IsVisible={isVisible}, {native}";
    }

    internal void CloseProbe()
    {
        _probe?.Close();
        _probe = null;
    }
}
