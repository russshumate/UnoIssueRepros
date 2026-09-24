namespace UnoIssueRepros.Issues.Issue14;

/// <summary>
/// MVUX generates a command for Probe(). The repro reports which thread it ran on and
/// whether touching a UI object there works.
/// </summary>
public partial record Issue14Model
{
    public IState<string> Report => State.Value(this, () => "Press the button.");

    public async ValueTask Probe(CancellationToken ct)
    {
        var text = ThreadReport.Describe("Command body");
        await Report.SetAsync(text, ct);
    }
}

/// <summary>The workaround: the model is given the UI DispatcherQueue and marshals UI work onto it.</summary>
public partial record Issue14WorkaroundModel(DispatcherQueue Dispatcher)
{
    public IState<string> Report => State.Value(this, () => "Press the button.");

    public async ValueTask Probe(CancellationToken ct)
    {
        var before = ThreadReport.Describe("Command body");
        var marshalled = await Dispatcher.RunOnUiAsync(() => ThreadReport.Describe("Inside Dispatcher.RunOnUiAsync"));
        await Report.SetAsync(before + Environment.NewLine + marshalled, ct);
    }
}

public static class ThreadReport
{
    public static string Describe(string where)
    {
        var onUiThread = DispatcherQueue.GetForCurrentThread() is not null;
        string touch;
        try
        {
            _ = new TextBlock { Text = "made here" };
            touch = "creating a TextBlock worked";
        }
        catch (Exception ex)
        {
            touch = $"creating a TextBlock threw {ex.GetType().Name}";
        }

        return $"{where}: managed thread {Environment.CurrentManagedThreadId}, " +
               $"{(onUiThread ? "UI thread (has a DispatcherQueue)" : "NOT the UI thread (no DispatcherQueue)")}, {touch}.";
    }
}

public static class DispatcherQueueExtensions
{
    /// <summary>Runs <paramref name="work"/> on the queue's thread and returns its result.</summary>
    public static Task<T> RunOnUiAsync<T>(this DispatcherQueue dispatcher, Func<T> work)
    {
        if (dispatcher.HasThreadAccess)
        {
            return Task.FromResult(work());
        }

        var completion = new TaskCompletionSource<T>();
        var queued = dispatcher.TryEnqueue(() =>
        {
            try
            {
                completion.SetResult(work());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });

        if (!queued)
        {
            completion.SetException(new InvalidOperationException("The dispatcher queue is shutting down."));
        }

        return completion.Task;
    }
}
