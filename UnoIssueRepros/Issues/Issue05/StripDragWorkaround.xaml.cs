using Windows.Foundation;

namespace UnoIssueRepros.Issues.Issue05;

public sealed partial class StripDragWorkaround : UserControl
{
    private const double DragThreshold = 6;

    private SelectorBarItem? _pressedItem;
    private Point _pressPoint;
    private bool _dragging;

    public StripDragWorkaround()
    {
        this.InitializeComponent();
        Bar.AddHandler(PointerPressedEvent, new PointerEventHandler(OnPressed), handledEventsToo: true);
        Bar.AddHandler(PointerMovedEvent, new PointerEventHandler(OnMoved), handledEventsToo: true);
        Bar.AddHandler(PointerReleasedEvent, new PointerEventHandler(OnReleased), handledEventsToo: true);
        StatusText.Text = "Drag a tab sideways.";
    }

    private void OnPressed(object sender, PointerRoutedEventArgs e)
    {
        _pressPoint = e.GetCurrentPoint(Bar).Position;
        _pressedItem = ItemAt(_pressPoint.X);
        _dragging = false;
    }

    private void OnMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_pressedItem is null)
        {
            return;
        }

        var point = e.GetCurrentPoint(Bar).Position;
        if (!_dragging && Math.Abs(point.X - _pressPoint.X) >= DragThreshold)
        {
            // Take the capture from the item so the rest of the gesture comes to the strip.
            _dragging = Bar.CapturePointer(e.Pointer);
            StatusText.Text = $"Dragging {_pressedItem.Text} (captured={_dragging})";
        }

        if (_dragging)
        {
            var target = ItemAt(point.X);
            StatusText.Text = $"Dragging {_pressedItem.Text} over {target?.Text ?? "(outside)"}";
        }
    }

    private void OnReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_dragging && _pressedItem is not null)
        {
            var target = ItemAt(e.GetCurrentPoint(Bar).Position.X);
            if (target is not null && target != _pressedItem)
            {
                var to = Bar.Items.IndexOf(target);
                Bar.Items.Remove(_pressedItem);
                Bar.Items.Insert(to, _pressedItem);
                StatusText.Text = $"Moved {_pressedItem.Text} to position {to + 1}.";
            }
            else
            {
                StatusText.Text = "Dropped in place.";
            }

            Bar.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        _pressedItem = null;
        _dragging = false;
    }

    private SelectorBarItem? ItemAt(double x)
    {
        foreach (var item in Bar.Items)
        {
            var left = item.TransformToVisual(Bar).TransformPoint(new Point(0, 0)).X;
            if (x >= left && x < left + item.ActualWidth)
            {
                return item;
            }
        }

        return null;
    }
}
