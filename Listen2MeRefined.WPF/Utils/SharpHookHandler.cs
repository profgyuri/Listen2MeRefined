using System;
using System.Threading;
using System.Threading.Tasks;

namespace Listen2MeRefined.WPF.Utils;

using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.WPF.Views;
using Serilog;
using SharpHook;
using SharpHook.Native;
using System.Collections.Generic;
using System.Windows.Forms;

internal sealed class SharpHookHandler
    : Listen2MeRefined.Infrastructure.IGlobalHook
{
    private readonly SharpHook.IGlobalHook hook;
    private readonly ILogger _logger;
    private readonly IMediaController _mediaController;
    private readonly System.Threading.Timer _mouseDebounceTimer;
    private Point _lastMousePosition;
    private const int DebounceInterval = 10; // ms
    private NewSongWindow? _window;

    private readonly int _width = Screen.PrimaryScreen!.Bounds.Width;
    private readonly int _height = Screen.PrimaryScreen.Bounds.Height;

    private const int TriggerNotificationWindowAreaSize = 10;

    private static readonly HashSet<KeyCode> _lowLevelKeys =
        [
            KeyCode.VcMediaPlay,
            KeyCode.VcMediaNext,
            KeyCode.VcMediaPrevious,
            KeyCode.VcMediaStop
        ];

    public SharpHookHandler(ILogger logger, IMediaController mediaController)
    {
        hook = new TaskPoolGlobalHook();
        _logger = logger;
        _mediaController = mediaController;
        
        _mouseDebounceTimer = new(CheckMousePosition, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task RegisterAsync()
    {
        try 
        {
            hook.KeyPressed += OnKeyDown;
            hook.MouseMoved += OnMouseMove;
            
            // Run hook on background thread
            await Task.Run(() => hook.Run());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize global hooks");
        }
    }

    private void OnMouseMove(object? sender, MouseHookEventArgs e)
    {
        var mouse = e.RawEvent.Mouse;
        _lastMousePosition = new Point(mouse.X, mouse.Y);
        
        // Debounce the mouse move handling
        _mouseDebounceTimer.Change(DebounceInterval, Timeout.Infinite);
    }

    private void CheckMousePosition(object? state)
    {
        var pos = _lastMousePosition;
        if (IsMouseInCorner(pos.X, pos.Y))
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => 
            {
                try
                {
                    _window = WindowManager.ShowNewSongWindow(pos.X, pos.Y);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to show new song window");
                }
            });
        }
        else
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => 
            {
                try
                {
                    WindowManager.CloseNewSongWindow(_window);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to close new song window");
                }
            });
        }
    }

    private bool IsMouseInCorner(int x, int y) =>
        (x <= TriggerNotificationWindowAreaSize || x >= _width - TriggerNotificationWindowAreaSize) &&
        (y <= TriggerNotificationWindowAreaSize || y >= _height - TriggerNotificationWindowAreaSize);

    private void OnKeyDown(object? sender, KeyboardHookEventArgs e)
    {
        if (!_lowLevelKeys.Contains(e.RawEvent.Keyboard.KeyCode))
        {
            return;
        }

        switch (e.RawEvent.Keyboard.KeyCode)
        {
            case KeyCode.VcMediaPlay:
                _mediaController.PlayPauseAsync();
                break;
            case KeyCode.VcMediaNext:
                _mediaController.NextAsync();
                break;
            case KeyCode.VcMediaPrevious:
                _mediaController.PreviousAsync();
                break;
            case KeyCode.VcMediaStop:
                _mediaController.Stop();
                break;
        }
    }

    public void Unregister()
    {
        hook.KeyPressed -= OnKeyDown;
        hook.MouseMoved -= OnMouseMove;
        hook.Dispose();
    }
}
