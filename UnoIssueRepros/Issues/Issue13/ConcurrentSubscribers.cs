using Uno.Extensions.Reactive.Core;
using UnoIssueRepros.Infrastructure;
using UnoIssueRepros.Issues.Shared;

namespace UnoIssueRepros.Issues.Issue13;

/// <summary>
/// Two subscribers on the same feed and the same SourceContext at the same time.
/// The state is set to 1..5; each subscriber should see every value.
/// </summary>
public static class ConcurrentSubscribers
{
    private const int Emissions = 5;

    public static async Task<(string Report, bool Reproduced)> RunReproAsync()
    {
        var lines = new List<string>();
        var reproduced = false;

        foreach (var (label, derive) in Variants())
        {
            var (a, b) = await RunTwoSubscribersAsync(derive, sharedContext: true);
            var lost = b.Values.Count < a.Values.Count;
            reproduced |= lost;
            lines.Add($"{label}{Environment.NewLine}  {a}{Environment.NewLine}  {b}{(lost ? "   ← second subscriber missed emissions" : string.Empty)}");
        }

        return (string.Join(Environment.NewLine, lines), reproduced);
    }

    public static async Task<string> RunWorkaroundsAsync()
    {
        var lines = new List<string>();

        // 1. Subscribe once and fan the values out to every consumer yourself.
        using (var cts = new CancellationTokenSource())
        {
            var owner = new object();
            var context = SourceContext.GetOrCreate(owner);
            IState<int> state;
            using (context.AsCurrent())
            {
                state = State.Value(owner, () => 0);
            }

            var consumerB = new List<int>();
            var single = new FeedSubscriber<int>("consumer A (the only subscription)", state, context, cts.Token);
            single.Received += value => { lock (consumerB) { consumerB.Add(value); } };
            await EmitAsync(state, cts.Token);
            await cts.CancelAsync();
            await FeedHarness.StopAsync(single.Completion);
            lines.Add($"One subscription, fanned out{Environment.NewLine}  {single}{Environment.NewLine}  consumer B (fed by A) received {consumerB.Count}: [{string.Join(", ", consumerB)}]");
        }

        // 2. Give each subscriber its own SourceContext.
        var (a, b) = await RunTwoSubscribersAsync(state => state, sharedContext: false);
        lines.Add($"Each subscriber on its own SourceContext{Environment.NewLine}  {a}{Environment.NewLine}  {b}");

        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<(string Label, Func<IState<int>, IFeed<int>> Derive)> Variants()
    {
        yield return ("The state itself", state => state);
        yield return ("A derived feed: state.Select(x => x * 10)", state => state.Select(x => x * 10));
        yield return ("A derived async feed: state.SelectAsync(...)", state => state.SelectAsync(async (x, ct) =>
        {
            await Task.Delay(10, ct);
            return x * 100;
        }));
    }

    private static async Task<(FeedSubscriber<int> A, FeedSubscriber<int> B)> RunTwoSubscribersAsync(
        Func<IState<int>, IFeed<int>> derive,
        bool sharedContext)
    {
        using var cts = new CancellationTokenSource();
        var owner = new object();
        var contextA = SourceContext.GetOrCreate(owner);
        var contextB = sharedContext ? contextA : SourceContext.GetOrCreate(new object());

        IState<int> state;
        IFeed<int> feed;
        using (contextA.AsCurrent())
        {
            state = State.Value(owner, () => 0);
            feed = derive(state);
        }

        var a = new FeedSubscriber<int>("subscriber A", feed, contextA, cts.Token);
        var b = new FeedSubscriber<int>("subscriber B", feed, contextB, cts.Token);
        await EmitAsync(state, cts.Token);
        await cts.CancelAsync();
        await FeedHarness.StopAsync(a.Completion, b.Completion);
        return (a, b);
    }

    private static async Task EmitAsync(IState<int> state, CancellationToken ct)
    {
        await FeedHarness.Beat();
        for (var i = 1; i <= Emissions; i++)
        {
            await state.UpdateAsync(_ => i, ct);
            await FeedHarness.Beat();
        }
    }
}

public sealed class ConcurrentSubscribersPane : UserControl
{
    public ConcurrentSubscribersPane(bool workaround)
    {
        var result = Ui.Result();
        var verdict = Ui.Result();

        Content = Ui.Stack(
            Ui.Para(workaround
                ? "Two ways that deliver every value to both consumers: subscribe once and fan out, or give each " +
                  "subscriber its own SourceContext. Which one fits depends on whether the consumers must share the " +
                  "feed's cache and its single execution."
                : "Two subscribers enumerate the same feed on the same SourceContext at the same time, while the " +
                  "state is set to 1, 2, 3, 4, 5. Expected: both see every value. In Reactive 7.1.1 the second " +
                  "subscriber never received anything. The probe runs headless, with no UI binding involved."),
            Ui.Button(workaround ? "Run the workarounds" : "Run the probe", async () =>
            {
                result.Text = "Running…";
                if (workaround)
                {
                    result.Text = await ConcurrentSubscribers.RunWorkaroundsAsync();
                }
                else
                {
                    var (report, reproduced) = await ConcurrentSubscribers.RunReproAsync();
                    result.Text = report;
                    Ui.SetVerdict(verdict, reproduced, reproduced
                        ? "a second concurrent subscriber missed emissions."
                        : "both subscribers saw every emission on this version.");
                }
            }),
            verdict,
            result);
    }
}
