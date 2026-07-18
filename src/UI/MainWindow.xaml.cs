using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace DofusPolyfocus;

public sealed class SlotView
{
    public required int Number { get; init; }
    public required nint Handle { get; init; }
    public required string DisplayName { get; init; }
    public required string ClassInitials { get; init; }
    public required Brush ClassBrush { get; init; }
    public ImageSource? ClassIcon { get; init; }
    public FontWeight FontWeight { get; init; }
}

public partial class MainWindow : FluentWindow
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

        _registry = new AccountRegistry(ProcessNameFragment);
        _registry.Changed += RefreshList;

        var hotkeyConfig = AppConfig.LoadOrCreateDefault();
        _hotkeys = new HotkeyManager(this);
        var failed = new List<string>();

        void TryRegister(string binding, Action onPressed)
        {
            var (modifiers, key) = HotkeyBinding.Parse(binding);
            if (!_hotkeys.Register(modifiers, key, onPressed))
            {
                failed.Add(binding);
            }
        }

        for (int i = 0; i < hotkeyConfig.Slots.Length; i++)
        {
            int slotNumber = i + 1;
            TryRegister(hotkeyConfig.Slots[i], () => _registry.ActivateSlot(slotNumber));
        }

        TryRegister(hotkeyConfig.Next, () => _registry.ActivateRelative(1));
        TryRegister(hotkeyConfig.Previous, () => _registry.ActivateRelative(-1));

        HotkeyHint.Text = $"{hotkeyConfig.Slots[0]}..{hotkeyConfig.Slots[^1]} select · " +
                           $"{hotkeyConfig.Next} / {hotkeyConfig.Previous} rotate · click to switch";

        if (failed.Count > 0)
        {
            System.Windows.MessageBox.Show(
                $"These hotkeys can't be registered :\n{string.Join('\n', failed)}",
                "Dofus Polyfocus",
                System.Windows.MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void RefreshList()
    {
        if (_registry is null) return;

        nint foreground = Native.GetForegroundWindow();
        SlotsList.ItemsSource = _registry.Slots.Select(s =>
        {
            var (name, className) = ClassBadge.ParseTitle(s.Title);
            return new SlotView
            {
                Number = s.Number,
                Handle = s.Handle,
                DisplayName = $"{s.Number}. {name}",
                ClassInitials = ClassBadge.Initials(className),
                ClassBrush = ClassBadge.BrushFor(className),
                ClassIcon = ClassBadge.TryGetIcon(className),
                FontWeight = s.Handle == foreground ? FontWeights.Bold : FontWeights.Normal,
            };
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
