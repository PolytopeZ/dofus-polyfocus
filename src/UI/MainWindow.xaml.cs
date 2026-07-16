using System.Windows;

namespace DofusPolyfocus;

public sealed class SlotView
{
    public required int Number { get; init; }
    public required nint Handle { get; init; }
    public required string Label { get; init; }
    public FontWeight FontWeight { get; init; }
}

public partial class MainWindow : Window
{
    private const string ProcessNameFragment = "dofus";
    private AccountRegistry? _registry;
    private HotkeyManager? _hotkeys;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _registry = new AccountRegistry(ProcessNameFragment, TimeSpan.FromMilliseconds(500));
        _registry.Changed += RefreshList;

        _hotkeys = new HotkeyManager(this);
        for (int i = 1; i <= 9; i++)
        {
            int slotNumber = i;
            _hotkeys.Register(Native.MOD_CONTROL, (uint)('0' + i), () => _registry.ActivateSlot(slotNumber));
        }

        const uint VK_TAB = 0x09;
        _hotkeys.Register(Native.MOD_CONTROL, VK_TAB, () => _registry.ActivateRelative(1));
        _hotkeys.Register(Native.MOD_CONTROL | Native.MOD_SHIFT, VK_TAB, () => _registry.ActivateRelative(-1));
    }

    private void RefreshList()
    {
        if (_registry is null) return;

        nint foreground = Native.GetForegroundWindow();
        SlotsList.ItemsSource = _registry.Slots.Select(s => new SlotView
        {
            Number = s.Number,
            Handle = s.Handle,
            Label = $"{s.Number}. {s.Title}",
            FontWeight = s.Handle == foreground ? FontWeights.Bold : FontWeights.Normal,
        }).ToList();
    }

    private void SlotsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SlotsList.SelectedItem is SlotView slot)
        {
            WindowActivator.Activate(slot.Handle);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _hotkeys?.Dispose();
        _registry?.Dispose();
        base.OnClosed(e);
    }
}
