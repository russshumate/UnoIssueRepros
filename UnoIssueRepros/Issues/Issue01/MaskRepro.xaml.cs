using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue01;

public sealed partial class MaskRepro : UserControl, IScriptedRepro
{
    private const string Digits = "5551234567";
    private const string Expected = "(555) 123-4567";

    public MaskRepro()
    {
        this.InitializeComponent();
        MaskedBox.TextChanged += (_, _) =>
        {
            ValueText.Text = $"Text = \"{MaskedBox.Text}\"";
            ChildMode.Report($"TextChanged: \"{MaskedBox.Text}\"");
        };
    }

    /// <summary>
    /// Types 5551234567 with real keyboard messages posted to this window, so the keystrokes
    /// take the same path as a user typing. Needs Windows; elsewhere, type by hand.
    /// </summary>
    public async Task RunAsync()
    {
        var hwnd = Win32.FindByTitle(App.MainWindow?.Title ?? string.Empty)?.Handle ?? 0;
        if (hwnd == 0)
        {
            ChildMode.Report("RESULT inconclusive: scripted typing needs the Win32 host; open the child and type by hand");
            return;
        }

        ChildMode.Report($"typing {Digits} with WM_KEYDOWN/WM_CHAR/WM_KEYUP posted to window 0x{hwnd:X}");

        // Control first: proves the posted keystrokes reach a TextBox at all.
        await TypeIntoAsync(PlainBox, hwnd);
        ChildMode.Report($"control box (no mask) now reads \"{PlainBox.Text}\"");
        if (PlainBox.Text != Digits)
        {
            ChildMode.Report("RESULT inconclusive: the posted keystrokes did not reach the control box either");
            return;
        }

        await TypeIntoAsync(MaskedBox, hwnd);
        var ok = MaskedBox.Text == Expected;
        ChildMode.Report(ok
            ? $"RESULT not reproduced: masked box reads \"{MaskedBox.Text}\""
            : $"RESULT reproduced: masked box reads \"{MaskedBox.Text}\", expected \"{Expected}\"; the same keystrokes typed correctly into the control box");
        await Task.Delay(300);
    }

    private static async Task TypeIntoAsync(TextBox box, nint hwnd)
    {
        box.Focus(FocusState.Programmatic);
        await Task.Delay(400);
        foreach (var digit in Digits)
        {
            Win32.PostKeystroke(hwnd, digit);
            await Task.Delay(150);
        }

        await Task.Delay(400);
    }
}
