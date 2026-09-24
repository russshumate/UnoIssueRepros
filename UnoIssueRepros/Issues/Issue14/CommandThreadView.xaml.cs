namespace UnoIssueRepros.Issues.Issue14;

public sealed partial class CommandThreadView : UserControl
{
    public CommandThreadView() : this(workaround: false)
    {
    }

    public CommandThreadView(bool workaround)
    {
        this.InitializeComponent();
        UiThreadText.Text = $"This view was built on managed thread {Environment.CurrentManagedThreadId} (the UI thread).";

        if (workaround)
        {
            IntroText.Text =
                "Give the model the UI DispatcherQueue (here through its constructor) and marshal anything that " +
                "touches UI objects onto it. Keep the rest of the command off the UI thread; that part is a feature.";
            DataContext = new Issue14WorkaroundViewModel(DispatcherQueue.GetForCurrentThread());
        }
        else
        {
            IntroText.Text =
                "MVUX runs generated commands off the UI thread. That is fine for I/O, but code in the command that " +
                "touches UI (a dialog, a picker, an ObservableCollection bound to a ListView) runs on the wrong thread. " +
                "On Skia desktop even creating a control there does not throw, which makes it easy to miss. Press the " +
                "button and compare the thread with the one above.";
            DataContext = new Issue14ViewModel();
        }
    }
}
