using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using UnoIssueRepros.Infrastructure;

// Uno0001: RenderTargetBitmap is not implemented on WebAssembly; the snapshot then returns null
// and the page falls back to "compare by eye".
#pragma warning disable Uno0001

namespace UnoIssueRepros.Issues.Issue07;

public sealed partial class UnderlineRepro : UserControl
{
    public UnderlineRepro()
    {
        this.InitializeComponent();
        Loaded += async (_, _) =>
        {
            await Task.Delay(300);
            await CompareAsync();
        };
    }

    /// <summary>Counts pixels that differ between the plain sample and each decorated sample.</summary>
    internal async Task CompareAsync()
    {
        var plain = await SnapshotAsync(PlainSample);
        await Report(UnderlineVerdict, "Underline", plain, await SnapshotAsync(UnderlineSample));
        await Report(StrikeVerdict, "Strikethrough", plain, await SnapshotAsync(StrikeSample));
    }

    internal string Verdicts => $"{UnderlineVerdict.Text} {StrikeVerdict.Text}";

    private static Task Report(TextBlock target, string decoration, byte[]? plain, byte[]? decorated)
    {
        if (plain is null || decorated is null)
        {
            target.Text = $"{decoration}: snapshot unavailable, compare by eye.";
            return Task.CompletedTask;
        }

        var ink = 0;
        for (var i = 0; i < plain.Length; i += 4)
        {
            if (plain[i] + plain[i + 1] + plain[i + 2] < 300)
            {
                ink++;
            }
        }

        if (ink == 0)
        {
            target.Text = $"{decoration}: the snapshot captured no text at all, so it cannot decide; compare by eye.";
            return Task.CompletedTask;
        }

        var differing = 0;
        for (var i = 0; i < Math.Min(plain.Length, decorated.Length); i += 4)
        {
            if (Math.Abs(plain[i] - decorated[i]) + Math.Abs(plain[i + 1] - decorated[i + 1]) + Math.Abs(plain[i + 2] - decorated[i + 2]) > 60)
            {
                differing++;
            }
        }

        var missing = differing == 0 && plain.Length == decorated.Length;
        Ui.SetVerdict(target, missing, missing
            ? $"{decoration} renders nothing: the decorated text is pixel-identical to the plain text ({ink} text pixels compared)."
            : $"{decoration} changed {differing} pixel(s) compared with the plain text.");
        return Task.CompletedTask;
    }

    private static async Task<byte[]?> SnapshotAsync(UIElement element)
    {
        try
        {
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(element);
            return (await bitmap.GetPixelsAsync()).ToArray();
        }
        catch
        {
            return null;
        }
    }
}
