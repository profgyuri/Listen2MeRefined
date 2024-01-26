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
    }

    public void Register()
    {
        hook.KeyPressed += OnKeyDown;
        hook.MouseMoved += OnMouseMove;
        hook.RunAsync().ConfigureAwait(false);
    }

    private void OnMouseMove(object? sender, MouseHookEventArgs e)
    {
        var mouse = e.RawEvent.Mouse;
        var shouldShowNewWindow =
            (mouse.X <= TriggerNotificationWindowAreaSize ||
            mouse.X >= _width - TriggerNotificationWindowAreaSize) &&
            (mouse.Y <= TriggerNotificationWindowAreaSize || mouse.Y >=
            _height - TriggerNotificationWindowAreaSize);

        if (shouldShowNewWindow)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _window = WindowManager.ShowNewSongWindow(mouse.X, mouse.Y);
            });
        }
        else if (_window is not null && System.Windows.Application.Current is not null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                WindowManager.CloseNewSongWindow(_window);
            });
        }
    }

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
