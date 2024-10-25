namespace Listen2MeRefined.Infrastructure.SystemOperations.KeyboardHook;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

public sealed class KeyboardHook : IDisposable
{
    public KeyboardHook(HashSet<ConsoleKey> registeredKeys)
    {
        _registeredKeys = registeredKeys;

        _windowsHookHandle = IntPtr.Zero;
        _user32LibraryHandle = IntPtr.Zero;
        _hookProc = LowLevelKeyboardProc; // we must keep alive _hookProc, because GC is not aware about SetWindowsHookEx behaviour.

        _user32LibraryHandle = LoadLibrary("User32");
        if (_user32LibraryHandle == IntPtr.Zero)
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode,
                $"Failed to load library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
        }

        _windowsHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, _user32LibraryHandle,
            0);
        if (_windowsHookHandle != IntPtr.Zero)
        {
            return;
        }

        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode,
                $"Failed to adjust keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
        }
    }

    private IntPtr LowLevelKeyboardProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        var fEatKeyStroke = false;

        var wparamTyped = wParam.ToInt32();
        if (!Enum.IsDefined(typeof(KeyboardState), wparamTyped))
        {
            return fEatKeyStroke
                ? 1
                : CallNextHookEx(IntPtr.Zero, nCode, wParam,
                    lParam);
        }

        var o = Marshal.PtrToStructure(lParam, typeof(KeycodeInterpreter));
        var p = o is null
            ? throw new NullReferenceException("Object cannot be null!")
            : (KeycodeInterpreter)o;

        var eventArguments = new KeyboardHookEventArgs(p, (KeyboardState)wparamTyped);

        var key = (ConsoleKey)p.VirtualCode;
        //If the constructor gets null, the program can become a keylogger!!!
        if (_registeredKeys?.Contains(key) == false)
        {
            return fEatKeyStroke
                ? 1
                : CallNextHookEx(IntPtr.Zero, nCode, wParam,
                    lParam);
        }

        var handler = KeyboardPressed;
        handler?.Invoke(this, eventArguments);

        fEatKeyStroke = eventArguments.Handled;

        return fEatKeyStroke
            ? 1
            : CallNextHookEx(IntPtr.Zero, nCode, wParam,
                lParam);
    }

    private delegate IntPtr HookProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    #region Variables and constants
    private IntPtr _windowsHookHandle;
    private IntPtr _user32LibraryHandle;
    private HookProc _hookProc;

    private const int WH_KEYBOARD_LL = 13;

    public event EventHandler<KeyboardHookEventArgs>? KeyboardPressed;

    private readonly HashSet<ConsoleKey> _registeredKeys;
    #endregion

    #region Disposal
    private void Dispose(bool disposing)
    {
        if (disposing && _windowsHookHandle != IntPtr.Zero)
        {
            if (!UnhookWindowsHookEx(_windowsHookHandle))
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode,
                    $"Failed to remove keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }

            _windowsHookHandle = IntPtr.Zero;

#pragma warning disable CS8601
            _hookProc -= LowLevelKeyboardProc;
#pragma warning restore CS8601
        }

        if (_user32LibraryHandle == IntPtr.Zero)
        {
            return;
        }

        if (!FreeLibrary(_user32LibraryHandle)) // reduces reference to library by 1.
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode,
                $"Failed to unload library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
        }

        _user32LibraryHandle = IntPtr.Zero;
    }

    ~KeyboardHook()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region External dll imports
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern bool FreeLibrary(IntPtr hModule);

    /// <summary>
    ///     The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain.
    ///     You would install a hook procedure to monitor the system for certain types of events. These events are
    ///     associated either with a specific thread or with all threads in the same desktop as the calling thread.
    /// </summary>
    /// <param name="idHook">hook type</param>
    /// <param name="lpfn">hook procedure</param>
    /// <param name="hMod">handle to application instance</param>
    /// <param name="dwThreadId">thread identifier</param>
    /// <returns>If the function succeeds, the return value is the handle to the hook procedure.</returns>
    [DllImport("USER32", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        HookProc lpfn,
        IntPtr hMod,
        int dwThreadId);

    /// <summary>
    ///     The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx
    ///     function.
    /// </summary>
    /// <param name="hHook">handle to hook procedure</param>
    /// <returns>If the function succeeds, the return value is true.</returns>
    [DllImport("USER32", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hHook);

    /// <summary>
    ///     The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain.
    ///     A hook procedure can call this function either before or after processing the hook information.
    /// </summary>
    /// <param name="hHook">handle to current hook</param>
    /// <param name="code">hook code passed to hook procedure</param>
    /// <param name="wParam">value passed to hook procedure</param>
    /// <param name="lParam">value passed to hook procedure</param>
    /// <returns>If the function succeeds, the return value is true.</returns>
    [DllImport("USER32", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
        IntPtr hHook,
        int code,
        IntPtr wParam,
        IntPtr lParam);
    #endregion
}