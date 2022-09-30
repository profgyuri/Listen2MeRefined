namespace Listen2MeRefined.WPF;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

/// <summary>
///     Fixes the issue with Windows of Style <see cref="WindowStyle.None" /> covering the taskbar. Do not use MaxWidth and
///     MaxHeight!
/// </summary>
public class WindowResizer
{
    /// <summary>
    ///     The window to handle the resizing for
    /// </summary>
    private readonly Window _mWindow;

    /// <summary>
    ///     Default constructor
    /// </summary>
    /// <param name="window">The window to monitor and correctly maximize</param>
    public WindowResizer(Window window)
    {
        _mWindow = window;

        // Listen out for source initialized to setup
        _mWindow.SourceInitialized += Window_SourceInitialized;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, MonitorInfo lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr MonitorFromPoint(Point pt, MonitorOptions dwFlags);

    /// <summary>
    ///     Initialize and hook into the windows message pump
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        // Get the handle of this window
        var handle = new WindowInteropHelper(_mWindow).Handle;
        var handleSource = HwndSource.FromHwnd(handle);

        // Hook into it's Windows messages
        handleSource?.AddHook(WindowProc);
    }

    /// <summary>
    ///     Listens out for all windows messages for this window
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="msg"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <param name="handled"></param>
    /// <returns></returns>
    private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam,
        IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            // Handle the GetMinMaxInfo of the Window
            case 0x0024: /* WM_GETMINMAXINFO */
                WmGetMinMaxInfo(lParam);
                handled = true;
                break;
        }

        return (IntPtr)0;
    }

    /// <summary>
    ///     Get the min/max window size for this window
    ///     Correctly accounting for the taskbar size and position
    /// </summary>
    /// <param name="lParam"></param>
    private static void WmGetMinMaxInfo(IntPtr lParam)
    {
        GetCursorPos(out var lMousePosition);

        var lPrimaryScreen = MonitorFromPoint(new Point(0, 0), MonitorOptions.MonitorDefaulttoprimary);
        MonitorInfo lPrimaryScreenInfo = new();
        if (!GetMonitorInfo(lPrimaryScreen, lPrimaryScreenInfo))
        {
            return;
        }

        var lCurrentScreen = MonitorFromPoint(lMousePosition, MonitorOptions.MonitorDefaulttonearest);

        var lMmi = (MinMaxInfo)Marshal.PtrToStructure(lParam, typeof(MinMaxInfo))!;

        if (lPrimaryScreen.Equals(lCurrentScreen))
        {
            lMmi.ptMaxPosition.X = lPrimaryScreenInfo.rcWork.Left;
            lMmi.ptMaxPosition.Y = lPrimaryScreenInfo.rcWork.Top;
            lMmi.ptMaxSize.X = lPrimaryScreenInfo.rcWork.Right - lPrimaryScreenInfo.rcWork.Left;
            lMmi.ptMaxSize.Y = lPrimaryScreenInfo.rcWork.Bottom - lPrimaryScreenInfo.rcWork.Top;
        }
        else
        {
            lMmi.ptMaxPosition.X = lPrimaryScreenInfo.rcMonitor.Left;
            lMmi.ptMaxPosition.Y = lPrimaryScreenInfo.rcMonitor.Top;
            lMmi.ptMaxSize.X = lPrimaryScreenInfo.rcMonitor.Right - lPrimaryScreenInfo.rcMonitor.Left;
            lMmi.ptMaxSize.Y = lPrimaryScreenInfo.rcMonitor.Bottom - lPrimaryScreenInfo.rcMonitor.Top;
        }

        // Now we have the max size, allow the host to tweak as needed
        Marshal.StructureToPtr(lMmi, lParam, true);
    }
}

public enum MonitorOptions : uint
{
    MonitorDefaulttoprimary = 0x00000001,
    MonitorDefaulttonearest = 0x00000002
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public sealed class MonitorInfo
{
    public Rectangle rcMonitor = new();
    public Rectangle rcWork = new();
}

[StructLayout(LayoutKind.Sequential)]
public struct Rectangle
{
    public readonly int Left;
    public readonly int Top;
    public readonly int Right;
    public readonly int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
public struct MinMaxInfo
{
    private readonly Point ptReserved;
    public Point ptMaxSize;
    public Point ptMaxPosition;
    private readonly Point ptMinTrackSize;
    private readonly Point ptMaxTrackSize;
}

[StructLayout(LayoutKind.Sequential)]
public struct Point
{
    public int X;
    public int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}