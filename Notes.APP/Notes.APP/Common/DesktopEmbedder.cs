using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

public static class DesktopEmbedder
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private static IntPtr workerw = IntPtr.Zero;
    private static IntPtr petWindow = IntPtr.Zero;
    private static DispatcherTimer monitorTimer;

    public static void StartEmbedding(Window window)
    {
        petWindow = new WindowInteropHelper(window).Handle;

        // 避免出现在任务栏或 Alt+Tab
        HideFromTaskbar(petWindow);

        Embed();

        // 启动定时检查机制
        monitorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        monitorTimer.Tick += (_, _) =>
        {
            if (!IsWorkerWValid())
            {
                Embed();
            }
        };
        monitorTimer.Start();
    }

    private static void Embed()
    {
        IntPtr progman = FindWindow("Progman", null);
        SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

        workerw = IntPtr.Zero;

        EnumWindows((tophandle, _) =>
        {
            IntPtr shellView = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
                workerw = FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
            return true;
        }, IntPtr.Zero);

        if (workerw != IntPtr.Zero)
        {
            SetParent(petWindow, workerw);
        }
    }

    private static bool IsWorkerWValid()
    {
        return workerw != IntPtr.Zero && FindWindowEx(workerw, IntPtr.Zero, "SHELLDLL_DefView", null) == IntPtr.Zero;
    }

    private static void HideFromTaskbar(IntPtr hwnd)
    {
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TOOLWINDOW);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
