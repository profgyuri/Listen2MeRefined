using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using Serilog;

namespace Listen2MeRefined.WPF;

internal class GmaGlobalHookHandler : IGlobalHook
{
    private readonly ILogger _logger;
    private readonly IMediaController _mediaController;
    private readonly IKeyboardMouseEvents _globalHook = Hook.GlobalEvents();
    
    private static readonly HashSet<Keys> _lowLevelKeys =
        new()
        {
            Keys.MediaPlayPause,
            Keys.MediaNextTrack,
            Keys.MediaPreviousTrack,
            Keys.MediaStop,
        };

    public GmaGlobalHookHandler(ILogger logger, IMediaController mediaController)
    {
        _logger = logger;
        _mediaController = mediaController;
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
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
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
    
    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (
            e.X <= 0 || e.X >= Screen.PrimaryScreen.Bounds.Width - 5
            || e.Y <= 0 || e.Y >= Screen.PrimaryScreen.Bounds.Height - 5)
        {
            WindowManager.ShowNewSongWindow(e.X, e.Y);
        }
    }
}