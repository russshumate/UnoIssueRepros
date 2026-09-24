using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue22;

public partial record GreetingRecord(string Text);

public partial record Issue22Model
{
    public IFeed<string> Greeting => Feed.Async(async ct =>
    {
        await Task.Delay(300, ct);
        return "Hello from a feed";
    });

    public IFeed<GreetingRecord> WrappedGreeting => Feed.Async(async ct =>
    {
        await Task.Delay(300, ct);
        return new GreetingRecord("Hello from a feed of a record");
    });
}

/// <summary>
/// With bindable generation v3 (the template default), the generated view model exposes an
/// IFeed&lt;string&gt; as a plain string property. A FeedView bound to it receives the string,
/// not the feed, and shows its error state. A feed of a record is exposed as a bindable
/// wrapper that FeedView understands.
/// </summary>
public sealed partial class StringFeedView : UserControl
{
    public StringFeedView() : this(workaround: false)
    {
    }

    public StringFeedView(bool workaround)
    {
        this.InitializeComponent();
        IntroText.Text = workaround
            ? "Either wrap the value in a record (a FeedView bound to IFeed<GreetingRecord> works, second box), or skip the " +
              "FeedView and bind the value directly (third line). If you need FeedView's loading and error states for a " +
              "primitive, the record wrapper is the simplest route."
            : "The model has an IFeed<string>. The project uses [assembly: BindableGenerationTool(3)], which is what " +
              "dotnet new unoapp generates. The generated view model exposes the feed as a string property, so the " +
              "FeedView's Source becomes the string itself and the FeedView shows its error state, with no exception and no log line.";
        DataContext = new Issue22ViewModel();
        Loaded += async (_, _) =>
        {
            await Task.Delay(1000);
            Report();
        };
    }

    internal void Report()
    {
        var stringSource = StringFeed.Source?.GetType().Name ?? "null";
        var recordSource = RecordFeed.Source?.GetType().Name ?? "null";
        ResultText.Text =
            $"IFeed<string> FeedView: Source is {stringSource}, state has data: {StringFeed.State?.Data is not null}{Environment.NewLine}" +
            $"IFeed<GreetingRecord> FeedView: Source is {recordSource}, state has data: {RecordFeed.State?.Data is not null}";

        var reproduced = StringFeed.Source is string;
        Ui.SetVerdict(VerdictText, reproduced, reproduced
            ? "the FeedView bound to IFeed<string> received a string instead of a feed."
            : "the FeedView bound to IFeed<string> received a feed.");
    }
}
