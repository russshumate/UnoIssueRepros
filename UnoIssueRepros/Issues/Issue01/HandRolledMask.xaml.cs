using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue01;

public sealed partial class HandRolledMask : UserControl
{
    private const int MaxDigits = 10;
    private bool _formatting;

    public HandRolledMask()
    {
        this.InitializeComponent();
    }

    /// <summary>Formats up to ten digits as (999) 999-9999, growing as the user types.</summary>
    public static string Format(string digits) => digits.Length switch
    {
        0 => string.Empty,
        <= 3 => $"({digits}",
        <= 6 => $"({digits[..3]}) {digits[3..]}",
        _ => $"({digits[..3]}) {digits[3..6]}-{digits[6..]}",
    };

    private void OnBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        if (!_formatting && args.NewText.Any(c => !char.IsDigit(c) && "() -".IndexOf(c) < 0))
        {
            args.Cancel = true;
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_formatting)
        {
            return;
        }

        var digits = new string(PhoneBox.Text.Where(char.IsDigit).Take(MaxDigits).ToArray());
        var formatted = Format(digits);
        if (formatted != PhoneBox.Text)
        {
            _formatting = true;
            PhoneBox.Text = formatted;
            PhoneBox.SelectionStart = formatted.Length;
            _formatting = false;
        }

        ResultText.Text = $"Text = \"{PhoneBox.Text}\"  digits = {digits}";
    }

    private async void OnSimulate(object sender, RoutedEventArgs e)
    {
        PhoneBox.Text = string.Empty;
        foreach (var digit in "5551234567")
        {
            var caret = PhoneBox.SelectionStart;
            PhoneBox.Text = PhoneBox.Text.Insert(caret, digit.ToString());
            PhoneBox.SelectionStart = Math.Min(caret + 1, PhoneBox.Text.Length);
            await Task.Delay(60);
        }

        var ok = PhoneBox.Text == "(555) 123-4567";
        ResultText.Text = $"Text = \"{PhoneBox.Text}\" → {(ok ? "formatted as expected" : "unexpected result")}";
    }

    private void OnClear(object sender, RoutedEventArgs e) => PhoneBox.Text = string.Empty;
}
