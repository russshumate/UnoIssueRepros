using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;

namespace UnoIssueRepros.Infrastructure;

/// <summary>Small builders for the chrome around the repros, so it stays out of the repro XAML.</summary>
public static class Ui
{
    public static readonly FontFamily Mono = new("Consolas, Menlo, monospace");

    public static TextBlock Heading(string text) => new()
    {
        Text = text,
        FontSize = 16,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 12, 0, 4),
        TextWrapping = TextWrapping.WrapWholeWords,
    };

    public static TextBlock Para(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.WrapWholeWords,
        IsTextSelectionEnabled = true,
        Margin = new Thickness(0, 2, 0, 6),
    };

    public static Border Code(string text) => new()
    {
        Background = Brush("SubtleFillColorSecondaryBrush"),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(8, 6, 8, 6),
        Margin = new Thickness(0, 2, 0, 8),
        Child = new TextBlock
        {
            Text = text,
            FontFamily = Mono,
            FontSize = 13,
            IsTextSelectionEnabled = true,
            TextWrapping = TextWrapping.Wrap,
        },
    };

    public static TextBlock Result(string text = "") => new()
    {
        Text = text,
        FontFamily = Mono,
        FontSize = 13,
        TextWrapping = TextWrapping.Wrap,
        IsTextSelectionEnabled = true,
        Margin = new Thickness(0, 4, 0, 4),
    };

    public static Button Button(string text, Action onClick)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 4, 8, 4) };
        button.Click += (_, _) => onClick();
        return button;
    }

    public static Button Button(string text, Func<Task> onClick)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 4, 8, 4) };
        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            try { await onClick(); }
            finally { button.IsEnabled = true; }
        };
        return button;
    }

    public static StackPanel Row(params UIElement[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var child in children)
        {
            panel.Children.Add(child);
        }
        return panel;
    }

    public static StackPanel Stack(params UIElement[] children)
    {
        var panel = new StackPanel { Spacing = 2 };
        foreach (var child in children)
        {
            panel.Children.Add(child);
        }
        return panel;
    }

    /// <summary>A verdict line: green when the issue did not reproduce, red when it did.</summary>
    public static void SetVerdict(TextBlock target, bool reproduced, string text)
    {
        target.Text = (reproduced ? "REPRODUCED: " : "Not reproduced: ") + text;
        target.Foreground = reproduced
            ? Brush("SystemFillColorCriticalBrush")
            : Brush("SystemFillColorSuccessBrush");
    }

    public static Microsoft.UI.Xaml.Media.Brush Brush(string key) =>
        Application.Current.Resources.TryGetValue(key, out var value) && value is Microsoft.UI.Xaml.Media.Brush brush
            ? brush
            : new SolidColorBrush(Microsoft.UI.Colors.Gray);
}
