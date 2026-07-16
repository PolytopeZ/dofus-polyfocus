using System.Windows.Threading;

namespace DofusPolyfocus;

public sealed record AccountSlot(int Number, nint Handle, string Title);

public sealed class AccountRegistry : IDisposable
{
    private readonly string _processNameFragment;
    private readonly DispatcherTimer _timer;
    private readonly List<(nint Handle, string Title)> _tracked = new();

    public event Action? Changed;

    public IReadOnlyList<AccountSlot> Slots { get; private set; } = Array.Empty<AccountSlot>();

    public AccountRegistry(string processNameFragment, TimeSpan pollInterval)
    {
        _processNameFragment = processNameFragment;
        _timer = new DispatcherTimer { Interval = pollInterval };
        _timer.Tick += (_, _) => Poll();
        _timer.Start();
        Poll();
    }

    private void Poll()
    {
        var detected = WindowFinder.FindByProcessNameContains(_processNameFragment);
        var detectedHandles = detected.Select(d => d.Handle).ToHashSet();

        _tracked.RemoveAll(t => !detectedHandles.Contains(t.Handle));

        for (int i = 0; i < _tracked.Count; i++)
        {
            var match = detected.First(d => d.Handle == _tracked[i].Handle);
            _tracked[i] = (_tracked[i].Handle, match.Title);
        }

        var trackedHandles = _tracked.Select(t => t.Handle).ToHashSet();
        foreach (var d in detected)
        {
            if (!trackedHandles.Contains(d.Handle))
            {
                _tracked.Add((d.Handle, d.Title));
            }
        }

        Slots = _tracked.Select((t, i) => new AccountSlot(i + 1, t.Handle, t.Title)).ToArray();
        Changed?.Invoke();
    }

    public void ActivateSlot(int number)
    {
        var slot = Slots.FirstOrDefault(s => s.Number == number);
        if (slot is not null)
        {
            WindowActivator.Activate(slot.Handle);
        }
    }

    public void ActivateRelative(int offset)
    {
        if (Slots.Count == 0) return;

        nint foreground = Native.GetForegroundWindow();
        var slots = Slots;
        int currentIndex = -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Handle == foreground)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = currentIndex < 0 ? 0 : ((currentIndex + offset) % slots.Count + slots.Count) % slots.Count;
        WindowActivator.Activate(slots[nextIndex].Handle);
    }

    public void Dispose() => _timer.Stop();
}
