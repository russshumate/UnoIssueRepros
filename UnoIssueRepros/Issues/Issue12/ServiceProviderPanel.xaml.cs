using Uno.Extensions.Navigation.UI;

namespace UnoIssueRepros.Issues.Issue12;

public sealed partial class ServiceProviderPanel : UserControl
{
    public ServiceProviderPanel() : this(useWorkaround: false)
    {
    }

    public ServiceProviderPanel(bool useWorkaround)
    {
        this.InitializeComponent();
        UseWorkaround = useWorkaround;

        // Like an app that attaches services to its pages and to the controls its factory creates.
        if (App.Services is { } services)
        {
            Region.SetServiceProvider(this, services);
            Region.SetServiceProvider(FactoryProbe, services);
        }

        IntroText.Text = useWorkaround
            ? "Workaround: call this.FindServiceProvider() (namespace Uno.Extensions.Navigation), which walks up the " +
              "ancestors to the nearest attached provider. Every probe below now resolves the service."
            : "This panel has a service provider attached, the way an app attaches one to its pages. Each probe resolves " +
              "a service in Loaded with this.GetServiceProvider(). A control the code creates and attaches a provider to " +
              "works. A control built by an MVUX FeedView template gets null, so a cached null makes the feature silently do nothing.";
        NoteText.Text = useWorkaround
            ? "If no ancestor has a provider either (a control hosted outside any attached root), fall back to a static " +
              "provider captured at startup: this.FindServiceProvider() ?? App.Services."
            : "What we found while building this repro: GetServiceProvider() only reads the element's own " +
              "Region.ServiceProvider value; it does not look at ancestors. A control placed in XAML has the same " +
              "problem. The MVUX template is just where it bit us, because nothing there can attach a provider.";
        DataContext = new Issue12ViewModel();
    }

    /// <summary>Read by the probes, including those inside the template, which cannot take constructor arguments.</summary>
    public bool UseWorkaround { get; }
}
