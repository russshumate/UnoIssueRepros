using System.Runtime.InteropServices;
using System.Text;

namespace UnoIssueRepros.Infrastructure;

/// <summary>Just enough Win32 to inspect this process's top-level windows (issues 3, 4, 6).</summary>
public static class Win32
{
    public const string UnoWindowClass = "UnoPlatformRegularWindow";

    public static bool IsAvailable => OperatingSystem.IsWindows();

    public sealed record WindowInfo(nint Handle, string ClassName, string Title, bool Visible, long ExStyle)
    {
        public bool IsToolWindow => (ExStyle & WS_EX_TOOLWINDOW) != 0;

        public bool IsAppWindow => (ExStyle & WS_EX_APPWINDOW) != 0;

        public override string ToString() =>
            $"0x{Handle:X}  class={ClassName}  visible={Visible}  exstyle=0x{ExStyle:X}  \"{Title}\"";
    }

    public const long WS_EX_TOOLWINDOW = 0x00000080;
    public const long WS_EX_APPWINDOW = 0x00040000;

    /// <summary>All top-level windows owned by this process.</summary>
    public static List<WindowInfo> WindowsOfThisProcess()
    {
        var result = new List<WindowInfo>();
        if (!IsAvailable)
        {
            return result;
        }

        var pid = (uint)Environment.ProcessId;
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out var owner);
            if (owner == pid)
            {
                result.Add(Describe(hwnd));
            }
            return true;
        }, 0);
        return result;
    }

    public static WindowInfo Describe(nint hwnd)
    {
        var className = new StringBuilder(256);
        GetClassName(hwnd, className, className.Capacity);
        var title = new StringBuilder(512);
        GetWindowText(hwnd, title, title.Capacity);
        return new WindowInfo(hwnd, className.ToString(), title.ToString(), IsWindowVisible(hwnd), (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE));
    }

    public static WindowInfo? FindByTitle(string title) =>
        WindowsOfThisProcess().FirstOrDefault(w => w.ClassName == UnoWindowClass && w.Title == title);

    public static bool IsRealWindow(nint handle) => IsAvailable && IsWindow(handle);

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_CHAR = 0x0102;

    /// <summary>
    /// Types a digit or letter into a window of this process by posting keyboard messages to it.
    /// Unlike SendInput this cannot leak into another application's window.
    /// </summary>
    public static void PostKeystroke(nint hwnd, char c)
    {
        var vk = (nint)char.ToUpperInvariant(c);
        PostMessage(hwnd, WM_KEYDOWN, vk, 1);
        PostMessage(hwnd, WM_CHAR, c, 1);
        PostMessage(hwnd, WM_KEYUP, vk, unchecked((nint)0xC0000001));
    }

    private const int GWL_EXSTYLE = -20;

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hwnd, uint message, nint wParam, nint lParam);

    private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint hwnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hwnd, int index);
}
