using System.Runtime.InteropServices.WindowsRuntime;
using UnoIssueRepros.Infrastructure;

// Uno0001: RenderTargetBitmap is not implemented on WebAssembly; the snapshot then returns null
// and the page falls back to "compare by eye".
#pragma warning disable Uno0001

namespace UnoIssueRepros.Issues.Issue09;

/// <summary>
/// One control, two modes. The repro restores with a plain local set after the animation;
/// the workaround stops the storyboard first (after pinning the end value as a local value).
/// </summary>
public sealed partial class StoryboardHold : UserControl
{
    private readonly bool _stopBeforeRestore;

    public StoryboardHold() : this(stopBeforeRestore: false)
    {
    }

    public StoryboardHold(bool stopBeforeRestore)
    {
        this.InitializeComponent();
        _stopBeforeRestore = stopBeforeRestore;
        IntroText.Text = stopBeforeRestore
            ? "Workaround: in Completed, pin the end state as a local value and call Stop(). The local Opacity = 1 then takes effect."
            : "Fade the box out with a Storyboard, then restore it with Box.Opacity = 1 (a local value). A completed " +
              "Storyboard holds its value (FillBehavior HoldEnd), and WinUI's precedence rules put animated values above " +
              "local ones, so the box should stay invisible. In our app a recycled list row did stay invisible. The check " +
              "below snapshots the pixels, because the Opacity property can read 1 while nothing is drawn.";
    }

    private async void OnRun(object sender, RoutedEventArgs e) => await RunAsync();

    /// <summary>Runs the sequence; returns true when the box is still invisible after the restore.</summary>
    internal async Task<bool> RunAsync()
    {
        var completed = new TaskCompletionSource();
        void OnCompleted(object? s, object args) => completed.TrySetResult();
        FadeOut.Completed += OnCompleted;
        FadeOut.Begin();
        await completed.Task;
        FadeOut.Completed -= OnCompleted;
        await Task.Delay(50);
        var drawnWhileFaded = await IsBoxDrawnAsync();

        if (_stopBeforeRestore)
        {
            Box.Opacity = 0;   // pin the end state locally so Stop() does not snap back to 1 mid-flow
            FadeOut.Stop();
        }

        Box.Opacity = 1;
        await Task.Delay(50);

        var local = Box.ReadLocalValue(OpacityProperty);
        var drawn = await IsBoxDrawnAsync();
        ResultText.Text = $"Faded: snapshot says the box is {Describe(drawnWhileFaded)}. " +
                          $"After Box.Opacity = 1: Box.Opacity reads {Box.Opacity:0.##}, local value {local}, snapshot says the box is {Describe(drawn)}.";

        // The property can read 1 while the held animation still decides what is drawn, so trust the pixels.
        var reproduced = drawn is null ? Box.Opacity < 0.5 : !drawn.Value;
        Ui.SetVerdict(VerdictText, reproduced, reproduced
            ? "the held animation value (0) overrides the local set; the box is invisible."
            : "the box is visible again.");
        return reproduced;
    }

    private static string Describe(bool? drawn) => drawn is null ? "unknown (snapshot unavailable)" : drawn.Value ? "drawn" : "NOT drawn";

    /// <summary>Renders the stage and checks whether the centre pixel is the box's blue or the white background.</summary>
    private async Task<bool?> IsBoxDrawnAsync()
    {
        try
        {
            var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
            await bitmap.RenderAsync(Stage);
            var pixels = (await bitmap.GetPixelsAsync()).ToArray();
            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            if (width == 0 || height == 0 || pixels.Length < width * height * 4)
            {
                return null;
            }

            var i = ((height / 2) * width + (width / 2)) * 4;   // BGRA
            var (b, g, r) = (pixels[i], pixels[i + 1], pixels[i + 2]);
            return b > 120 && r < 100;   // blue box, not white background
        }
        catch
        {
            return null;
        }
    }

    internal string Readout => ResultText.Text;

    private void OnReset(object sender, RoutedEventArgs e)
    {
        FadeOut.Stop();
        Box.Opacity = 1;
        ResultText.Text = string.Empty;
        VerdictText.Text = string.Empty;
    }
}
