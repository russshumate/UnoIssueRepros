using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue11;

public sealed record ParentViewModel(string Title);

public sealed record ChildViewModel(string Title);

/// <summary>
/// A child control that wants its own view model. In repro mode it uses <c>DataContext ??=</c>;
/// in workaround mode it caches the view model in a field and assigns it unconditionally.
/// </summary>
public sealed partial class DataContextTrap : UserControl
{
    private readonly bool _useWorkaround;
    private ChildViewModel? _viewModel;

    public DataContextTrap() : this(useWorkaround: false)
    {
    }

    public DataContextTrap(bool useWorkaround)
    {
        this.InitializeComponent();
        _useWorkaround = useWorkaround;
        ModeText.Text = useWorkaround
            ? "OnLoaded: _viewModel ??= new ChildViewModel(...); DataContext = _viewModel;"
            : "OnLoaded: DataContext ??= new ChildViewModel(...);";
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_useWorkaround)
        {
            // Resolve once, assign every time: the inherited value is always replaced.
            _viewModel ??= new ChildViewModel("child view model");
            DataContext = _viewModel;
        }
        else
        {
            // The trap: DataContext is inherited, so by Loaded it already holds the parent's view model.
            DataContext ??= new ChildViewModel("child view model");
        }

        var ownVm = DataContext is ChildViewModel;
        Ui.SetVerdict(ResultText, !ownVm, ownVm
            ? "the child is bound to its own view model."
            : $"DataContext is {DataContext?.GetType().Name ?? "null"}, inherited from the parent; the child's view model was never created.");
    }
}

public static class Issue11Panes
{
    public static UIElement Repro() => Host(useWorkaround: false,
        "The parent sets DataContext to its own view model. The child sets DataContext ??= new ChildViewModel() " +
        "in Loaded. Because DataContext is inherited, ??= sees the parent's view model and never creates the child's. " +
        "Every binding in the child silently resolves against the wrong object. With MVUX the child's feeds are cold, " +
        "so nothing ever subscribes and a FeedView spins forever. This is standard WinUI inheritance; the ask is guidance.");

    public static UIElement Workaround() => Host(useWorkaround: true,
        "Cache the view model in a field and assign DataContext unconditionally. You still resolve it once, " +
        "and the inherited value is always replaced.");

    private static UIElement Host(bool useWorkaround, string intro)
    {
        var parent = new StackPanel
        {
            Spacing = 6,
            DataContext = new ParentViewModel("parent view model (wrong for the child)"),
        };
        parent.Children.Add(Ui.Para(intro));
        parent.Children.Add(new DataContextTrap(useWorkaround));
        return parent;
    }
}
