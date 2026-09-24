using System.Net.Http;
using System.Text.Json;
using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue16;

/// <summary>Asks nuget.org which versions of Reactive and Reactive.Testing exist.</summary>
public sealed class TestingPackagePane : UserControl
{
    private static readonly string[] Packages =
    [
        "Uno.Extensions.Reactive",
        "Uno.Extensions.Reactive.Testing",
    ];

    public TestingPackagePane()
    {
        var result = Ui.Result();
        var verdict = Ui.Result();

        Content = Ui.Stack(
            Ui.Para("Uno.Extensions.Reactive is on 7.x, but Uno.Extensions.Reactive.Testing (FeedTests, .Record()) " +
                    "stopped at 2.x and depends on Reactive 2.x, so it cannot be used with 7.x. There is no supported " +
                    "way to unit-test MVUX models."),
            Ui.Para("Check it live against nuget.org:"),
            Ui.Button("Query nuget.org", async () =>
            {
                result.Text = "Querying…";
                try
                {
                    var latest = new Dictionary<string, string>();
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                    var lines = new List<string>();
                    foreach (var package in Packages)
                    {
                        var json = await http.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{package.ToLowerInvariant()}/index.json");
                        var versions = JsonDocument.Parse(json).RootElement.GetProperty("versions")
                            .EnumerateArray().Select(v => v.GetString()!).Where(v => !v.Contains('-')).ToList();
                        latest[package] = versions.LastOrDefault() ?? "(none)";
                        lines.Add($"{package}: {versions.Count} stable versions, latest {latest[package]}");
                    }

                    result.Text = string.Join(Environment.NewLine, lines);
                    var major = (string p) => int.TryParse(latest[p].Split('.')[0], out var m) ? m : 0;
                    var behind = major(Packages[1]) < major(Packages[0]);
                    Ui.SetVerdict(verdict, behind, behind
                        ? "the testing package's latest major version is behind Reactive's."
                        : "a testing package matching Reactive's major version exists now.");
                }
                catch (Exception ex)
                {
                    result.Text = $"Could not reach nuget.org: {ex.GetType().Name}: {ex.Message}";
                }
            }),
            verdict,
            result);
    }
}

public static class Issue16Panes
{
    public static UIElement Workaround() => new StepsView("""
        Drive feeds with the public API instead. It is what issues 13 and 15 in this app use, so those pages are live examples.
        ## The pattern
        $ var owner = new object();
        $ var context = SourceContext.GetOrCreate(owner);        // Uno.Extensions.Reactive.Core
        $ IState<int> state;
        $ using (context.AsCurrent())
        $ {
        $     state = State.Value(owner, () => 0);
        $ }
        $
        $ // Subscribe by pumping the feed on a background task.
        $ var values = new List<int>();
        $ _ = Task.Run(async () =>
        $ {
        $     await foreach (var message in state.GetSource(context, ct))
        $         if (message.Current.Data.IsSome(out var value)) values.Add(value);
        $ });
        $
        $ await state.UpdateAsync(_ => 42, ct);
        ## Things this turned up
        - A selection used as a Combine input re-runs the feed on every change (issue 15).
        - Awaiting a state inside a feed's SelectAsync body is not a re-run dependency: the feed runs once.
        - A null reference value is Option.None. Never await a feed that can be null: it waits forever.
        - Feed.Combine with a None input emits None and does not call the projection.
        ## The ask
        Ship Uno.Extensions.Reactive.Testing for 7.x, or document SourceContext + GetSource as the supported test seam.
        """);
}
