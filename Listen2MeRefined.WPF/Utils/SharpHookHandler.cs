using System.Collections.Generic;
using Listen2MeRefined.WPF.Views;
using SharpHook;
using SharpHook.Data;
using System.Windows.Forms;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Infrastructure.Settings;
using Serilog;
using IGlobalHook = Listen2MeRefined.Application.Utils.IGlobalHook;
using Timer = System.Threading.Timer;

namespace Listen2MeRefined.WPF.Utils;

internal sealed class SharpHookHandler : IGlobalHook
{
    private const int DefaultTriggerAreaSize = 10;
    private const int MinTriggerAreaSize = 4;
    private const int MaxTriggerAreaSize = 64;
    private const int DefaultDebounceMs = 10;
    private const int MinDebounceMs = 5;
    private const int MaxDebounceMs = 200;

    private static readonly HashSet<KeyCode> LowLevelKeys =
    [
        KeyCode.VcMediaPlay,
        KeyCode.VcMediaNext,
        KeyCode.VcMediaPrevious,
        KeyCode.VcMediaStop
    ];

    private readonly ILogger _logger;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IWindowManager _windowManager;
    private readonly IUiDispatcher _ui;
    private readonly Timer _mouseDebounceTimer;
    private readonly object _registrationGate = new();
    private readonly int _width = Screen.PrimaryScreen!.Bounds.Width;
    private readonly int _height = Screen.PrimaryScreen.Bounds.Height;

    private readonly SharpHook.IGlobalHook _hook;
    private bool _handlersAttached;
    private bool _runLoopStarted;
    private Point _lastMousePosition;
    private CornerWindow? _window;

    public SharpHookHandler(
        ILogger logger,
        IMusicPlayerController musicPlayerController,
        ISettingsManager<AppSettings> settingsManager, 
        IWindowManager windowManager,
        IUiDispatcher ui)
    {
        _hook = new TaskPoolGlobalHook();
        _logger = logger;
        _musicPlayerController = musicPlayerController;
        _settingsManager = settingsManager;
        _windowManager = windowManager;
        _ui = ui;
        _mouseDebounceTimer = new Timer(CheckMousePosition, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task RegisterAsync()
    {
        var shouldStartRunLoop = false;
        lock (_registrationGate)
        {
            if (_handlersAttached)
            {
                return Task.CompletedTask;
            }

            _hook.KeyPressed += OnKeyDown;
            _hook.MouseMoved += OnMouseMove;
            _handlersAttached = true;
            shouldStartRunLoop = !_runLoopStarted;
            _runLoopStarted = true;
        }

        if (!shouldStartRunLoop)
        {
            return Task.CompletedTask;
        }

        _ = Task.Run(() => _hook.Run()).ContinueWith(task =>
        {
            _logger.Error(task.Exception, "Failed to initialize global hooks");
            lock (_registrationGate)
            {
                _runLoopStarted = false;
            }
        }, TaskContinuationOptions.OnlyOnFaulted);

        return Task.CompletedTask;
    }

    public void Unregister()
    {
        lock (_registrationGate)
        {
            if (!_handlersAttached)
            {
                return;
            }

            _hook.KeyPressed -= OnKeyDown;
            _hook.MouseMoved -= OnMouseMove;
            _handlersAttached = false;
        }

        HideNowPlayingWindow();
    }

    private void OnMouseMove(object? sender, MouseHookEventArgs e)
    {
        if (!IsCornerNowPlayingPopupEnabled())
        {
            HideNowPlayingWindow();
            return;
        }

        var mouse = e.RawEvent.Mouse;
        _lastMousePosition = new Point(mouse.X, mouse.Y);
        _mouseDebounceTimer.Change(GetDebounceIntervalMs(), Timeout.Infinite);
    }

    private void CheckMousePosition(object? state)
    {
        if (!IsCornerNowPlayingPopupEnabled())
        {
            HideNowPlayingWindow();
            return;
        }

        var pos = _lastMousePosition;
        var triggerSize = GetTriggerAreaSizePx();
        if (IsMouseInCorner(pos.X, pos.Y, triggerSize))
        {
            _ui.InvokeAsync(() =>
            {
                try
                {
                    var options = WindowShowOptions.At(pos.X, pos.Y);
                    _windowManager.ShowWindowAsync<CornerWindowShellViewModel>(options);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to show new song window");
                }
            });
            return;
        }

        HideNowPlayingWindow();
    }

    private void OnKeyDown(object? sender, KeyboardHookEventArgs e)
    {
        if (!IsGlobalMediaKeysEnabled() ||
            !LowLevelKeys.Contains(e.RawEvent.Keyboard.KeyCode))
        {
            return;
        }

        switch (e.RawEvent.Keyboard.KeyCode)
        {
            case KeyCode.VcMediaPlay:
                _musicPlayerController.PlayPauseAsync();
                break;
            case KeyCode.VcMediaNext:
                _musicPlayerController.NextAsync();
                break;
            case KeyCode.VcMediaPrevious:
                _musicPlayerController.PreviousAsync();
                break;
            case KeyCode.VcMediaStop:
                _musicPlayerController.Stop();
                break;
        }
    }

    private bool IsMouseInCorner(int x, int y, int triggerAreaSize) =>
        (x <= triggerAreaSize || x >= _width - triggerAreaSize) &&
        (y <= triggerAreaSize || y >= _height - triggerAreaSize);

    private bool IsGlobalMediaKeysEnabled()
    {
        return _settingsManager.Settings.EnableGlobalMediaKeys;
    }

    private bool IsCornerNowPlayingPopupEnabled()
    {
        return _settingsManager.Settings.EnableCornerNowPlayingPopup;
    }

    private int GetTriggerAreaSizePx()
    {
        var saved = _settingsManager.Settings.CornerTriggerSizePx;
        return Math.Clamp(saved == 0 ? DefaultTriggerAreaSize : saved, MinTriggerAreaSize, MaxTriggerAreaSize);
    }

    private int GetDebounceIntervalMs()
    {
        var saved = _settingsManager.Settings.CornerTriggerDebounceMs;
        return Math.Clamp(saved == 0 ? DefaultDebounceMs : saved, MinDebounceMs, MaxDebounceMs);
    }

    private void HideNowPlayingWindow()
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                _windowManager.CloseWindow<CornerWindowShellViewModel>();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to close new song window");
            }
        });
    }
}
