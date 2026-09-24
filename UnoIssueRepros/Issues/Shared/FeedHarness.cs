using Uno.Extensions.Reactive.Core;

namespace UnoIssueRepros.Issues.Shared;

/// <summary>
/// Drives MVUX feeds without UI and without a testing package (there is no
/// Uno.Extensions.Reactive.Testing for 7.x, see issue 16). A subscriber pumps
/// feed.GetSource(context) on a background task and records every value it sees.
/// </summary>
public sealed class FeedSubscriber<T>
{
    private readonly List<T> _values = [];

    public FeedSubscriber(string name, IFeed<T> feed, SourceContext context, CancellationToken ct)
    {
        Name = name;
        Completion = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in feed.GetSource(context, ct).WithCancellation(ct))
                {
                    if (message.Current.Data.IsSome(out var value))
                    {
                        lock (_values)
                        {
                            _values.Add(value);
                        }
                        Received?.Invoke(value);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }, CancellationToken.None);
    }

    public string Name { get; }

    public Task Completion { get; }

    public event Action<T>? Received;

    public IReadOnlyList<T> Values
    {
        get
        {
            lock (_values)
            {
                return [.. _values];
            }
        }
    }

    /// <summary>True once the subscription has ended after cancellation.</summary>
    public bool Stopped => Completion.IsCompleted;

    public override string ToString() =>
        $"{Name} received {Values.Count}: [{string.Join(", ", Values)}]" +
        (Stopped ? string.Empty : "  (its enumeration did not end after cancellation)");
}

public static class FeedHarness
{
    /// <summary>Time for subscribers to attach, or for a change to propagate.</summary>
    public static Task Beat() => Task.Delay(150);

    /// <summary>Waits for subscriptions to end, but never forever: a stuck one is reported, not awaited.</summary>
    public static Task StopAsync(params Task[] completions) =>
        Task.WhenAny(Task.WhenAll(completions), Task.Delay(TimeSpan.FromSeconds(2)));
}
