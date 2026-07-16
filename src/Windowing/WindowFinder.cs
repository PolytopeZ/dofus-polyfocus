using System.Diagnostics;
using System.Text;

namespace DofusPolyfocus;

public sealed record DetectedWindow(nint Handle, string Title);

public static class WindowFinder
{
    public static List<DetectedWindow> FindByProcessNameContains(string processNameFragment)
    {
        var results = new List<DetectedWindow>();
        int ownPid = Environment.ProcessId;

        Native.EnumWindows((hWnd, _) =>
        {
            if (!Native.IsWindowVisible(hWnd)) return true;

            int length = Native.GetWindowTextLength(hWnd);
            if (length == 0) return true;


            var sb = new StringBuilder(length + 1);
            Native.GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();

            Native.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == ownPid) return true;


            string processName;
            try
            {
                processName = Process.GetProcessById((int)pid).ProcessName;
            }
            catch (ArgumentException)
            {
                return true;
            }

            if (processName.Contains(processNameFragment, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new DetectedWindow(hWnd, title));
            }

            return true;
        }, 0);

        return results;
    }
}
