using System.Text;

namespace UnoIssueRepros.Infrastructure;

/// <summary>A read-only, monospace, append-only log that is safe to call from any thread.</summary>
public sealed class LogView : UserControl
{
    private const int MaxChars = 60_000;

    private readonly TextBox _box;
    private readonly StringBuilder _text = new();
    private readonly DispatcherQueue _dispatcher;

    public LogView(double height = 180)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _box = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = Ui.Mono,
            FontSize = 12,
            Height = height,
            Margin = new Thickness(0, 4, 0, 4),
        };
        ScrollViewer.SetVerticalScrollBarVisibility(_box, ScrollBarVisibility.Visible);
        Content = _box;
    }

    public void Append(string line)
    {
        if (!_dispatcher.HasThreadAccess)
        {
            _dispatcher.TryEnqueue(() => Append(line));
            return;
        }

        _text.AppendLine($"{DateTime.Now:HH:mm:ss.fff}  {line}");
        if (_text.Length > MaxChars)
        {
            _text.Remove(0, _text.Length - MaxChars);
        }

        _box.Text = _text.ToString();
        _box.SelectionStart = _box.Text.Length;
    }

    public void Clear()
    {
        _text.Clear();
        _box.Text = string.Empty;
    }
}
