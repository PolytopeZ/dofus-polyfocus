using System.Windows;
using System.Windows.Interop;

namespace DofusPolyfocus;

public sealed class HotkeyManager : IDisposable
{
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 1;

    public HotkeyManager(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _source = HwndSource.FromHwnd(helper.Handle)
            ?? throw new InvalidOperationException("Window must be up!!!");
        _source.AddHook(WndProc);
    }

    public void Register(uint modifiers, uint virtualKey, Action onPressed)
    {
        int id = _nextId++;
        if (Native.RegisterHotKey(_source.Handle, id, modifiers, virtualKey) == 0)
        {
            throw new InvalidOperationException($"Failed to register hotkey id={id} !!!");
        }

        _handlers[id] = onPressed;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == Native.WM_HOTKEY && _handlers.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }

        return nint.Zero;
    }

    public void Dispose()
    {
        foreach (int id in _handlers.Keys)
        {
            Native.UnregisterHotKey(_source.Handle, id);
        }

        _handlers.Clear();
        _source.RemoveHook(WndProc);
    }
}
