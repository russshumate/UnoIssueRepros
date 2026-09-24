using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.UI;
using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue12;

public partial record Issue12Payload(string Text);

public partial record Issue12Model
{
    /// <summary>
    /// A trivial feed, so a FeedView renders its ValueTemplate. It carries a record, not a string:
    /// with bindable generation v3 an IFeed&lt;string&gt; is exposed as a plain string property, and a
    /// FeedView bound to it receives the string instead of the feed.
    /// </summary>
    public IFeed<Issue12Payload> Message => Feed.Async(async ct =>
    {
        await Task.Delay(300, ct);
        return new Issue12Payload("feed value");
    });
}

/// <summary>
/// Resolves a service in Loaded and shows what came back. In repro mode it calls
/// this.GetServiceProvider(), which reads only this element's own Region.ServiceProvider value.
/// In workaround mode it calls this.FindServiceProvider(), which walks up the ancestors.
/// </summary>
public sealed partial class ServiceProbe : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(ServiceProbe), new PropertyMetadata(string.Empty));

    private readonly TextBlock _text = Ui.Result();

    public ServiceProbe()
    {
        Content = _text;
        Loaded += OnLoaded;
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var useFind = FindPanel()?.UseWorkaround == true;
        var provider = useFind ? this.FindServiceProvider() : this.GetServiceProvider();
        var greeting = provider?.GetService<GreetingService>()?.Greet();

        var call = useFind ? "FindServiceProvider()" : "GetServiceProvider()";
        _text.Text = $"{Label}: {call} returned {(provider is null ? "null" : "a provider")}" +
                     (greeting is null ? " → service NOT resolved" : $" → \"{greeting}\"");
        _text.Foreground = Ui.Brush(greeting is null ? "SystemFillColorCriticalBrush" : "SystemFillColorSuccessBrush");
    }

    private ServiceProviderPanel? FindPanel()
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is ServiceProviderPanel panel)
            {
                return panel;
            }

            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
