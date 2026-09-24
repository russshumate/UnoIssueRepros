using UnoIssueRepros.Infrastructure;

namespace UnoIssueRepros.Issues.Issue05;

public sealed partial class ItemDragRepro : UserControl
{
    private readonly LogView _log = new(160);
    private int _itemPresses;
    private int _itemManipulations;
    private int _borderManipulations;

    public ItemDragRepro()
    {
        this.InitializeComponent();
        ((StackPanel)Content).Children.Add(_log);

        foreach (var item in Bar.Items)
        {
            var name = item.Text;
            item.AddHandler(PointerPressedEvent, new PointerEventHandler((_, e) =>
            {
                _itemPresses++;
                _log.Append($"{name}: PointerPressed (Handled={e.Handled}), captures={item.PointerCaptures?.Count ?? 0}");
                Update();
            }), handledEventsToo: true);
            item.ManipulationStarted += (_, _) =>
            {
                _itemManipulations++;
                _log.Append($"{name}: ManipulationStarted");
                Update();
            };
        }

        PlainBorder.ManipulationStarted += (_, _) =>
        {
            _borderManipulations++;
            _log.Append("Plain Border: ManipulationStarted");
            Update();
        };
        Update();
    }

    private void Update()
    {
        CountsText.Text = $"tab presses={_itemPresses}  tab manipulations={_itemManipulations}  border manipulations={_borderManipulations}";
        if (_itemPresses == 0)
        {
            VerdictText.Text = "Drag on a tab to test.";
            return;
        }

        var reproduced = _itemManipulations == 0;
        Ui.SetVerdict(VerdictText, reproduced, reproduced
            ? "tabs were pressed but no per-item manipulation started."
            : "per-item manipulation started on a tab.");
    }
}
