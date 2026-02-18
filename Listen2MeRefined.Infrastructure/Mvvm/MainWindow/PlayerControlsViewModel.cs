using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Mvvm.MainWindow;

public partial class PlayerControlsViewModel :
    ViewModelBase,
    INotificationHandler<CurrentSongNotification>
{
    private readonly ILogger _logger;
    private readonly IWaveFormDrawer<SKBitmap> _waveFormDrawer;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly TimedTask _timedTask;

    [ObservableProperty] private SKBitmap _waveForm = new(1, 1);
    [ObservableProperty] private int _waveFormWidth;
    [ObservableProperty] private int _waveFormHeight;
    [ObservableProperty] private double _totalTime;

    public double CurrentTime
    {
        get => _musicPlayerController.CurrentTime;
        set
        {
            _musicPlayerController.CurrentTime = value;
            OnPropertyChanged();
        }
    }

    public float Volume
    {
        get => _musicPlayerController.Volume;
        set
        {
            if (Math.Abs(_musicPlayerController.Volume - value) < float.Epsilon)
            {
                return;
            }

            _musicPlayerController.Volume = value;
            OnPropertyChanged();
        }
    }

    public PlayerControlsViewModel(
        ILogger logger,
        IWaveFormDrawer<SKBitmap> waveFormDrawer,
        IMusicPlayerController musicPlayerController,
        TimedTask timedTask)
    {
        _logger = logger;
        _waveFormDrawer = waveFormDrawer;
        _musicPlayerController = musicPlayerController;
        _timedTask = timedTask;

        _logger.Debug("[PlayerControlsViewModel] initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        _timedTask.Start(
            TimeSpan.FromMilliseconds(100),
            () => OnPropertyChanged(nameof(CurrentTime)));

        OnPropertyChanged(nameof(Volume));

        await Task.Run((Func<Task?>)(async () =>
        {
            WaveFormWidth = 480;
            WaveFormHeight = 70;
            _waveFormDrawer.SetSize(WaveFormWidth, WaveFormHeight);
            await DrawPlaceholderLineAsync();
        }), ct);

           _logger.Debug("[PlayerControlsViewModel] Finished InitializeCoreAsync");
    }

    [RelayCommand]
    private async Task PlayPause()
    {
        _logger.Debug("[PlayerControlsViewModel] Toggling play/pause");
        await _musicPlayerController.PlayPauseAsync();
    }

    [RelayCommand]
    private void Stop()
    {
        _logger.Debug("[PlayerControlsViewModel] Stopping playback");
        _musicPlayerController.Stop();
    }

    [RelayCommand]
    private async Task Next()
    {
        _logger.Debug("[PlayerControlsViewModel] Skipping to next track");
        await _musicPlayerController.NextAsync();
    }

    [RelayCommand]
    private async Task Previous()
    {
        _logger.Debug("[PlayerControlsViewModel] Skipping to previous track");
        await _musicPlayerController.PreviousAsync();
    }

    [RelayCommand]
    private async Task Shuffle()
    {
        _logger.Debug("[PlayerControlsViewModel] Shuffling playlist");
        await _musicPlayerController.Shuffle();
    }
    
    private async Task DrawPlaceholderLineAsync()
    {
        WaveForm = await _waveFormDrawer.LineAsync();
    }
    
    public async Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[PlayerControlsViewModel] Received CurrentSongNotification: {@Audio}", notification.Audio);
        await DrawPlaceholderLineAsync();
        WaveForm = await _waveFormDrawer.WaveFormAsync(notification.Audio.Path!);
        TotalTime = notification.Audio.Length.TotalMilliseconds;
    }
}
