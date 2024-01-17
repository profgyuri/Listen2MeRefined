namespace Listen2MeRefined.WPF;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using Listen2MeRefined.Infrastructure;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.WPF.Views;
using Serilog;

internal sealed class GmaGlobalHookHandler :
    IGlobalHook
{
    private readonly ILogger _logger;
    private readonly IMediaController _mediaController;
    private readonly IKeyboardMouseEvents _globalHook = Hook.GlobalEvents();
    private  NewSongWindow? _window;

    private readonly int _width = Screen.PrimaryScreen!.Bounds.Width;
    private readonly int _height = Screen.PrimaryScreen.Bounds.Height;

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
                _mediaController.PlayPauseAsync();
                break;
            case Keys.MediaNextTrack:
                _mediaController.NextAsync();
                break;
            case Keys.MediaPreviousTrack:
                _mediaController.PreviousAsync();
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
        var shouldShowNewWindow = 
            (e.X <= TriggerNotificationWindowAreaSize ||
            e.X >= _width - TriggerNotificationWindowAreaSize) && 
            (e.Y <= TriggerNotificationWindowAreaSize || e.Y >=
            _height - TriggerNotificationWindowAreaSize);

        if (shouldShowNewWindow)
        {
            _window = WindowManager.ShowNewSongWindow(e.X, e.Y);
        }
        else if (_window is not null)
        {
            WindowManager.CloseNewSongWindow(_window);
        }
    }

    #region Implementation of IGlobalHook
    /// <inheritdoc />
    public void Register()
    {
        _globalHook.KeyDown += OnKeyDown;
        _globalHook.MouseMove += OnMouseMove;
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