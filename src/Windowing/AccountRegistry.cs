using System.Diagnostics;

namespace DofusPolyfocus;

public sealed record AccountSlot(int Number, nint Handle, string Title);

public sealed class AccountRegistry : IDisposable
{
    private readonly string _processNameFragment;
    private readonly List<(nint Handle, string Title)> _tracked = new();
    private readonly Native.WinEventDelegate _hookCallback;
    private readonly nint[] _hooks;

    public event Action? Changed;

    public IReadOnlyList<AccountSlot> Slots { get; private set; } = Array.Empty<AccountSlot>();

    public AccountRegistry(string processNameFragment)
    {
        _processNameFragment = processNameFragment;
        _hookCallback = OnWinEvent;

        const uint flags = Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS;
        _hooks =
        [
            Native.SetWinEventHook(Native.EVENT_OBJECT_CREATE, Native.EVENT_OBJECT_CREATE, 0, _hookCallback, 0, 0, flags),
            Native.SetWinEventHook(Native.EVENT_OBJECT_DESTROY, Native.EVENT_OBJECT_DESTROY, 0, _hookCallback, 0, 0, flags),
            Native.SetWinEventHook(Native.EVENT_OBJECT_NAMECHANGE, Native.EVENT_OBJECT_NAMECHANGE, 0, _hookCallback, 0, 0, flags),
            Native.SetWinEventHook(Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND, 0, _hookCallback, 0, 0, flags),
        ];

        Poll();
    }

    private void OnWinEvent(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint eventThread, uint eventTime)
    {
        if (idObject != Native.OBJID_WINDOW || idChild != Native.CHILDID_SELF) return;

        bool relevant = eventType == Native.EVENT_SYSTEM_FOREGROUND
            || (eventType == Native.EVENT_OBJECT_DESTROY ? _tracked.Any(t => t.Handle == hwnd) : IsDofusWindow(hwnd));

        if (relevant)
        {
            Poll();
        }
    }

    private bool IsDofusWindow(nint hwnd)
    {
        Native.GetWindowThreadProcessId(hwnd, out uint pid);
        try
        {
            return Process.GetProcessById((int)pid).ProcessName.Contains(_processNameFragment, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
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

    public void Dispose()
    {
        foreach (var hook in _hooks)
        {
            Native.UnhookWinEvent(hook);
        }
    }
}
