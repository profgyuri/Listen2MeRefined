using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using Listen2MeRefined.WPF.Views;
using Serilog;

namespace Listen2MeRefined.WPF;

internal sealed class GmaGlobalHookHandler : IGlobalHook
{
    private readonly ILogger _logger;
    private readonly IMediaController _mediaController;
    private readonly IKeyboardMouseEvents _globalHook = Hook.GlobalEvents();
    private readonly HashSet<NewSongWindow> _windows = new();

    private const int TriggerNotificationWindowAreaSize = 10;

    private static readonly HashSet<Keys> _lowLevelKeys =
        new()
        {
            Keys.MediaPlayPause,
            Keys.MediaNextTrack,
            Keys.MediaPreviousTrack,
            Keys.MediaStop
        };

    public GmaGlobalHookHandler(
        ILogger logger,
        IMediaController mediaController)
    {
        _logger = logger;
        _mediaController = mediaController;
    }

    private void OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (!_lowLevelKeys.Contains(e.KeyCode))
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.MediaPlayPause:
                _mediaController.PlayPause();
                break;
            case Keys.MediaNextTrack:
                _mediaController.Next();
                break;
            case Keys.MediaPreviousTrack:
                _mediaController.Previous();
                break;
            case Keys.MediaStop:
                _mediaController.Stop();
                break;
        }
    }

    private void OnMouseMove(
        object? sender,
        MouseEventArgs e)
    {
        var shouldShowNewWindow = (e.X <= TriggerNotificationWindowAreaSize ||
                                   e.X >= Screen.PrimaryScreen!.Bounds.Width - TriggerNotificationWindowAreaSize)
                                  && (e.Y <= TriggerNotificationWindowAreaSize || e.Y >=
                                      Screen.PrimaryScreen!.Bounds.Height - TriggerNotificationWindowAreaSize);

        switch (shouldShowNewWindow)
        {
            case true when _windows.Count == 0:
            {
                var window = WindowManager.ShowNewSongWindow(e.X, e.Y);
                _windows.Add(window);
                return;
            }
            case true:
                return;
        }

        foreach (var window in _windows)
        {
            WindowManager.CloseNewSongWindow(window);
            _windows.Remove(window);
        }
    }

    #region Implementation of IGlobalHook
    /// <inheritdoc />
    public void Register()
    {
        try
        {
            _globalHook.KeyDown += OnKeyDown;
            _globalHook.MouseMove += OnMouseMove;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error registering global hook");
            Unregister();
        }
    }

    /// <inheritdoc />
    public void Unregister()
    {
        try
        {
            _globalHook.KeyDown -= OnKeyDown;
            _globalHook.MouseMove -= OnMouseMove;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error unregistering global hook");
        }
    }
    #endregion
}