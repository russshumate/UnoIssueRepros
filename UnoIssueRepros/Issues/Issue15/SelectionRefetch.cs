using System.Collections.Immutable;
using Uno.Extensions.Reactive.Core;
using UnoIssueRepros.Infrastructure;
using UnoIssueRepros.Issues.Shared;

namespace UnoIssueRepros.Issues.Issue15;

public sealed partial record Order(int Id, string Name);

/// <summary>
/// Counts how often the "server" is fetched while only the selection changes.
/// The selection id uses a non-nullable state with 0 as "none": a Combine input that is
/// null is Option.None, and the Combine then never runs its projection at all.
/// </summary>
public static class SelectionRefetch
{
    private const int SelectionChanges = 3;

    public static async Task<(string Report, int Fetches)> RunAntiPatternAsync()
    {
        using var cts = new CancellationTokenSource();
        var owner = new object();
        var context = SourceContext.GetOrCreate(owner);
        var fetches = 0;

        IState<string> filter;
        IState<int> selectedId;
        IFeed<ImmutableList<Order>> orders;
        using (context.AsCurrent())
        {
            filter = State.Value(owner, () => "open");
            selectedId = State.Value(owner, () => 0);

            // The anti-pattern: the selection is an input of the feed that fetches the list.
            orders = Feed.Combine(filter, selectedId).SelectAsync(async (inputs, ct) =>
            {
                Interlocked.Increment(ref fetches);
                return await FetchAsync(inputs.Item1, ct);
            });
        }

        var subscriber = new FeedSubscriber<ImmutableList<Order>>("orders", orders, context, cts.Token);
        await FeedHarness.Beat();
        var afterLoad = Volatile.Read(ref fetches);

        for (var id = 1; id <= SelectionChanges; id++)
        {
            await selectedId.UpdateAsync(_ => id, cts.Token);
            await FeedHarness.Beat();
        }

        await cts.CancelAsync();
        await FeedHarness.StopAsync(subscriber.Completion);
        var total = Volatile.Read(ref fetches);
        return ($"fetches after first load: {afterLoad}{Environment.NewLine}" +
                $"fetches after {SelectionChanges} selection changes: {total}", total - afterLoad);
    }

    public static async Task<(string Report, int Fetches)> RunPatternAsync()
    {
        using var cts = new CancellationTokenSource();
        var owner = new object();
        var context = SourceContext.GetOrCreate(owner);
        var fetches = 0;

        IState<string> filter;
        IState<int> selectedId;
        IFeed<ImmutableList<Order>> orders;
        IFeed<string> selectedName;
        using (context.AsCurrent())
        {
            filter = State.Value(owner, () => "open");
            selectedId = State.Value(owner, () => 0);

            // The list depends only on its real inputs.
            orders = filter.SelectAsync(async (f, ct) =>
            {
                Interlocked.Increment(ref fetches);
                return await FetchAsync(f, ct);
            });

            // The selected item is derived from the list and the id; changing the id does not refetch.
            selectedName = Feed.Combine(orders, selectedId)
                .Select(inputs => inputs.Item1.FirstOrDefault(o => o.Id == inputs.Item2)?.Name ?? "(none)");
        }

        var listSubscriber = new FeedSubscriber<ImmutableList<Order>>("orders", orders, context, cts.Token);
        var selectionSubscriber = new FeedSubscriber<string>("selected", selectedName, context, cts.Token);
        await FeedHarness.Beat();
        var afterLoad = Volatile.Read(ref fetches);

        for (var id = 1; id <= SelectionChanges; id++)
        {
            await selectedId.UpdateAsync(_ => id, cts.Token);
            await FeedHarness.Beat();
        }

        await cts.CancelAsync();
        await FeedHarness.StopAsync(listSubscriber.Completion, selectionSubscriber.Completion);
        var total = Volatile.Read(ref fetches);
        return ($"fetches after first load: {afterLoad}{Environment.NewLine}" +
                $"fetches after {SelectionChanges} selection changes: {total}{Environment.NewLine}" +
                $"{selectionSubscriber}", total - afterLoad);
    }

    private static async ValueTask<ImmutableList<Order>> FetchAsync(string filter, CancellationToken ct)
    {
        await Task.Delay(20, ct);
        return [new Order(1, $"{filter} order A"), new Order(2, $"{filter} order B"), new Order(3, $"{filter} order C")];
    }
}

public sealed class SelectionRefetchPane : UserControl
{
    public SelectionRefetchPane(bool workaround)
    {
        var result = Ui.Result();
        var verdict = Ui.Result();

        Content = Ui.Stack(
            Ui.Para(workaround
                ? "Keep the selection out of the list feed's inputs. The list depends only on the filter; the selected " +
                  "item is derived with Feed.Combine(orders, selectedId).Select(...). Selection changes now cost nothing."
                : "The list feed is Feed.Combine(filter, selectedId).SelectAsync(fetch). Every selection change re-runs " +
                  "the fetch, so a click in a list re-queries the server and rebuilds the list under the user. " +
                  "The probe changes only the selection, three times, and counts fetches."),
            Ui.Code(workaround
                ? "Orders   = Filter.SelectAsync(FetchAsync);\nSelected = Feed.Combine(Orders, SelectedId)\n               .Select(x => x.Item1.FirstOrDefault(o => o.Id == x.Item2));"
                : "Orders = Feed.Combine(Filter, SelectedId)\n             .SelectAsync((x, ct) => FetchAsync(x.Item1, ct));"),
            Ui.Button("Run the probe", async () =>
            {
                result.Text = "Running…";
                var (report, extraFetches) = workaround
                    ? await SelectionRefetch.RunPatternAsync()
                    : await SelectionRefetch.RunAntiPatternAsync();
                result.Text = report;
                Ui.SetVerdict(verdict, extraFetches > 0, extraFetches > 0
                    ? $"{extraFetches} extra fetch(es) caused by selection changes alone."
                    : "selection changes caused no fetches.");
            }),
            verdict,
            result,
            Ui.Para("Related rule: never make a Combine input a nullable state with a null default. Null is Option.None, " +
                    "Combine then never runs its projection, and no request is issued at all. Use a non-nullable state " +
                    "with a sentinel such as 0."));
    }
}
