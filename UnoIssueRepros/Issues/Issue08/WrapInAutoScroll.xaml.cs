using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue08;

public sealed partial class WrapInAutoScroll : UserControl, IScriptedRepro
{
    private const double Widest = 420;
    private const double Narrowest = 140;

    public WrapInAutoScroll() : this(ScrollBarVisibility.Auto)
    {
    }

    public WrapInAutoScroll(ScrollBarVisibility visibility)
    {
        this.InitializeComponent();
        Scroller.VerticalScrollBarVisibility = visibility;
        // Short enough to fit without scrolling when wide, long enough to overflow when narrow,
        // so the sweep crosses the width where the Auto scrollbar starts to be needed.
        WrappingText.Text = "A line-item note that wraps onto more lines as the column gets narrower, until it no longer fits.";
        ShowWidth();
    }

    public ScrollBarVisibility ScrollBarMode => Scroller.VerticalScrollBarVisibility;

    /// <summary>
    /// Scripted run: sweeps with a fixed Height, then with a MaxHeight (the viewer's own height then
    /// depends on the content, so the bar toggling feeds back into the parent's layout).
    /// </summary>
    public async Task RunAsync()
    {
        foreach (var (label, height, maxHeight) in new[] { ("Height=96", 96d, double.PositiveInfinity), ("MaxHeight=96", double.NaN, 96d) })
        {
            Scroller.Height = height;
            Scroller.MaxHeight = maxHeight;
            ChildMode.Report($"variant {label}");
            await SweepAsync(ChildMode.Report, CancellationToken.None);
        }
    }

    /// <summary>
    /// Walks the width down and back up one pixel at a time, then crawls 0.1 px at a time
    /// around every width where the scrollbar changed state.
    /// </summary>
    public async Task SweepAsync(Action<string> report, CancellationToken ct)
    {
        report($"sweeping width {Widest} → {Narrowest} → {Widest} with VerticalScrollBarVisibility={ScrollBarMode}");
        var toggles = new List<double>();
        var last = Scroller.ComputedVerticalScrollBarVisibility;

        for (var pass = 0; pass < 2; pass++)
        {
            for (var step = 0; step <= Widest - Narrowest; step++)
            {
                var width = pass == 0 ? Widest - step : Narrowest + step;
                await SetWidthAsync(width, ct);
                var now = Scroller.ComputedVerticalScrollBarVisibility;
                if (now != last)
                {
                    toggles.Add(width);
                    report($"scrollbar {last} → {now} at width {width}");
                    last = now;
                }
            }
        }

        foreach (var toggle in toggles.Distinct())
        {
            report($"fine sweep around {toggle}");
            for (var width = toggle - 3; width <= toggle + 3; width += 0.1)
            {
                await SetWidthAsync(Math.Round(width, 1), ct);
            }
        }

        report($"sweep finished ({toggles.Count} scrollbar changes)");
    }

    private async Task SetWidthAsync(double width, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Squeeze.Width = width;
        ShowWidth();
        await Task.Delay(15, ct);
    }

    private void ShowWidth() => WidthText.Text = $"container width {Squeeze.Width:0} px · VerticalScrollBarVisibility={Scroller.VerticalScrollBarVisibility}";
}

public sealed class LayoutCycleWorkaround : UserControl
{
    private readonly WrapInAutoScroll _safe = new(ScrollBarVisibility.Visible);
    private readonly LogView _log = new(120);
    private CancellationTokenSource? _cts;

    public LayoutCycleWorkaround()
    {
        Content = Ui.Stack(
            Ui.Para("Never put Auto over wrapping content that can be squeezed. Use Visible (the lane is always " +
                    "reserved, so the width never changes) or Hidden (the content still scrolls with wheel and touch). " +
                    "The same layout with Visible survives the same sweep, in this process."),
            Ui.Code("<ScrollViewer VerticalScrollBarVisibility=\"Visible\">  <!-- not Auto -->"),
            Ui.Row(Ui.Button("Run the sweep here", SweepAsync), Ui.Button("Stop", () => _cts?.Cancel())),
            _safe,
            _log,
            Ui.Heading("Finding the loop in your own app"),
            Ui.Para("Set Application.Current.DebugSettings.LayoutCycleTracingLevel = LayoutCycleTracingLevel.High at the " +
                    "top of OnLaunched, launch the exe with stdout redirected to a file, and count the elements that keep " +
                    "calling InvalidateMeasure. A desired size that flip-flops between two values is the bistable element."));
        Unloaded += (_, _) => _cts?.Cancel();
    }

    private async Task SweepAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        try
        {
            await _safe.SweepAsync(_log.Append, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            _log.Append("stopped");
        }
    }
}
