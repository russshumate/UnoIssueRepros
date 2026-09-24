using UnoIssueRepros.Infrastructure;
using UnoIssueRepros.Issues.Shared;

namespace UnoIssueRepros.Issues.Issue21;

/// <summary>Moves one live UIElement instance into a second window and back.</summary>
public sealed class MoveBetweenWindows : UserControl
{
    private readonly StackPanel _home = new() { Spacing = 8 };
    private readonly TextBlock _status = Ui.Result();
    private readonly Border _card;
    private readonly TextBlock _counterText = new() { FontSize = 18 };
    private int _counter;
    private Window? _other;

    public MoveBetweenWindows()
    {
        var increment = Ui.Button("Count", () => { _counter++; UpdateCounter(); });
        _card = new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(6),
            Background = Ui.Brush("SubtleFillColorSecondaryBrush"),
            Child = Ui.Stack(new TextBlock { Text = "One UIElement instance" }, _counterText, increment),
        };
        UpdateCounter();
        _home.Children.Add(_card);

        if (!WindowTools.MultiWindowSupported)
        {
            Content = Ui.Stack(WindowTools.DesktopOnlyNotice(), _home);
            return;
        }

        Content = Ui.Stack(
            Ui.Para("Solved: a UIElement can move from one Window to another (proven on 6.6.184). Count a few times, move the " +
                    "card to a new window, count there, and bring it back. The counter shows it is the same instance, with its state."),
            Ui.Row(Ui.Button("Move to a new window", MoveOut), Ui.Button("Bring it back", MoveBack)),
            _status,
            _home);
        Unloaded += (_, _) => MoveBack();
    }

    private void UpdateCounter() => _counterText.Text = $"Counted {_counter} time(s)";

    internal int Counter => _counter;

    internal bool IsHome => _home.Children.Contains(_card);

    internal void Count() { _counter++; UpdateCounter(); }

    internal void MoveOut()
    {
        if (_other is not null)
        {
            return;
        }

        try
        {
            _home.Children.Remove(_card);   // detach from the old parent first
            _other = WindowTools.CreateProbeWindow("Issue 21: moved element", string.Empty);
            _other.Content = new Border { Padding = new Thickness(16), Child = _card };
            _other.Closed += (_, _) => { if (_other is not null) { _other = null; Reattach(); } };
            _other.Activate();
            _status.Text = "Moved. Count in the other window, then bring it back.";
        }
        catch (Exception ex)
        {
            _status.Text = $"Moving threw {ex.GetType().Name}: {ex.Message}";
            Reattach();
        }
    }

    internal void MoveBack()
    {
        if (_other is null)
        {
            return;
        }

        var window = _other;
        _other = null;
        if (window.Content is Border host)
        {
            host.Child = null;   // detach from the other window's tree
        }
        window.Close();
        Reattach();
    }

    private void Reattach()
    {
        if (_card.Parent is Border host)
        {
            host.Child = null;
        }

        if (!_home.Children.Contains(_card))
        {
            _home.Children.Add(_card);
        }

        _status.Text = $"Back home. Same instance, counter still {_counter}.";
    }
}
