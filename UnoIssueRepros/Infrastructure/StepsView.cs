namespace UnoIssueRepros.Infrastructure;

/// <summary>
/// Renders written steps for the tooling rows. The input is a small line format:
/// "## " heading, "$ " a code line (consecutive ones join into one block),
/// "- " a bullet, a blank line is ignored, anything else is a paragraph.
/// </summary>
public sealed class StepsView : UserControl
{
    public StepsView(string steps)
    {
        var panel = new StackPanel { Spacing = 2 };
        var code = new List<string>();

        void FlushCode()
        {
            if (code.Count > 0)
            {
                panel.Children.Add(Ui.Code(string.Join(Environment.NewLine, code)));
                code.Clear();
            }
        }

        foreach (var raw in steps.Replace("\r", string.Empty).Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("$ ", StringComparison.Ordinal))
            {
                code.Add(line[2..]);
                continue;
            }

            FlushCode();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                panel.Children.Add(Ui.Heading(line[3..]));
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                var bullet = Ui.Para("•  " + line[2..]);
                bullet.Margin = new Thickness(8, 0, 0, 4);
                panel.Children.Add(bullet);
            }
            else
            {
                panel.Children.Add(Ui.Para(line));
            }
        }

        FlushCode();
        Content = panel;
    }
}
