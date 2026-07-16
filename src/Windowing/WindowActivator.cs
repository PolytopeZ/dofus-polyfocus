namespace DofusPolyfocus;

public static class WindowActivator
{
    public static void Activate(nint hWnd)
    {
        if (Native.IsIconic(hWnd))
        {
            Native.ShowWindow(hWnd, Native.SW_RESTORE);
        }

        if (Native.SetForegroundWindow(hWnd)) return;


        nint foreground = Native.GetForegroundWindow();
        uint foregroundThread = Native.GetWindowThreadProcessId(foreground, out _);
        uint currentThread = Native.GetCurrentThreadId();

        if (foregroundThread == currentThread) return;


        Native.AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            Native.SetForegroundWindow(hWnd);
        }
        finally
        {
            Native.AttachThreadInput(currentThread, foregroundThread, false);
        }
    }
}
