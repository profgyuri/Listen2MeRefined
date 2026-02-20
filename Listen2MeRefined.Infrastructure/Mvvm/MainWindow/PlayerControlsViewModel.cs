using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.Mvvm.MainWindow;

public partial class PlayerControlsViewModel :
    ViewModelBase,
    INotificationHandler<CurrentSongNotification>
{
    private const float VolumeEpsilon = 0.0001f;

    private readonly ILogger _logger;
    private readonly IWaveFormDrawer<SKBitmap> _waveFormDrawer;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly TimedTask _timedTask;
    private bool _isMuted;
    private float _lastNonZeroVolume = 0.7f;

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
            OnPropertyChanged(nameof(CurrentTimeDisplay));
        }
    }

    public TimeSpan TotalTimeDisplay => TimeSpan.FromMilliseconds(TotalTime);
    public TimeSpan CurrentTimeDisplay => TimeSpan.FromMilliseconds(CurrentTime);

    public float Volume
    {
        get => _musicPlayerController.Volume;
        set
        {
            var clampedValue = Math.Clamp(value, 0f, 1f);
            var previousVolume = _musicPlayerController.Volume;
            if (Math.Abs(previousVolume - clampedValue) < VolumeEpsilon)
            {
                return;
            }

            _musicPlayerController.Volume = clampedValue;
            if (clampedValue > VolumeEpsilon)
            {
                _lastNonZeroVolume = clampedValue;
            }

            if (_isMuted && clampedValue > VolumeEpsilon)
            {
                SetMuted(false);
            }
            else if (!_isMuted && clampedValue <= VolumeEpsilon)
            {
                SetMuted(true);
            }

            PersistPlaybackDefaults(clampedValue, _isMuted);
            OnPropertyChanged();
            OnPropertyChanged(nameof(VolumeIconKind));
        }
    }

    public bool IsMuted => _isMuted;

    public string VolumeIconKind =>
        _isMuted || Volume <= VolumeEpsilon
            ? "VolumeOff"
            : Volume < 0.34f
                ? "VolumeLow"
                : Volume < 0.67f
                    ? "VolumeMedium"
                    : "VolumeHigh";

    public PlayerControlsViewModel(
        ILogger logger,
        IWaveFormDrawer<SKBitmap> waveFormDrawer,
        IMusicPlayerController musicPlayerController,
        ISettingsManager<AppSettings> settingsManager,
        TimedTask timedTask)
    {
        _logger = logger;
        _waveFormDrawer = waveFormDrawer;
        _musicPlayerController = musicPlayerController;
        _settingsManager = settingsManager;
        _timedTask = timedTask;

        _logger.Debug("[PlayerControlsViewModel] initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        ApplyStartupPlaybackDefaults();

        _timedTask.Start(
            TimeSpan.FromMilliseconds(100),
            () =>
            {
                OnPropertyChanged(nameof(CurrentTime));
                OnPropertyChanged(nameof(CurrentTimeDisplay));
            });

        OnPropertyChanged(nameof(Volume));
        SetMuted(Volume <= VolumeEpsilon);
        if (Volume > VolumeEpsilon)
        {
            _lastNonZeroVolume = Volume;
        }

        OnPropertyChanged(nameof(VolumeIconKind));
        OnPropertyChanged(nameof(IsMuted));
        OnPropertyChanged(nameof(TotalTimeDisplay));

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

    [RelayCommand]
    private void ToggleMute()
    {
        if (_isMuted)
        {
            var restoredVolume = _lastNonZeroVolume > VolumeEpsilon ? _lastNonZeroVolume : 0.7f;
            _musicPlayerController.Volume = restoredVolume;
            SetMuted(false);
            PersistPlaybackDefaults(restoredVolume, isMuted: false);
            OnPropertyChanged(nameof(Volume));
            OnPropertyChanged(nameof(VolumeIconKind));
            return;
        }

        var currentVolume = Volume;
        if (currentVolume > VolumeEpsilon)
        {
            _lastNonZeroVolume = currentVolume;
        }

        _musicPlayerController.Volume = 0f;
        SetMuted(true);
        PersistPlaybackDefaults(0f, isMuted: true);
        OnPropertyChanged(nameof(Volume));
        OnPropertyChanged(nameof(VolumeIconKind));
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
        OnPropertyChanged(nameof(TotalTimeDisplay));
    }

    private void SetMuted(bool isMuted)
    {
        if (_isMuted == isMuted)
        {
            return;
        }

        _isMuted = isMuted;
        OnPropertyChanged(nameof(IsMuted));
        OnPropertyChanged(nameof(VolumeIconKind));
    }

    private void ApplyStartupPlaybackDefaults()
    {
        var settings = _settingsManager.Settings;
        var startupVolume = Math.Clamp(settings.StartupVolume, 0f, 1f);
        if (startupVolume > VolumeEpsilon)
        {
            _lastNonZeroVolume = startupVolume;
        }

        var startsMuted = settings.StartMuted;
        _musicPlayerController.Volume = startsMuted ? 0f : startupVolume;
        SetMuted(startsMuted || startupVolume <= VolumeEpsilon);
    }

    private void PersistPlaybackDefaults(float currentVolume, bool isMuted)
    {
        _settingsManager.SaveSettings(settings =>
        {
            settings.StartMuted = isMuted;
            if (currentVolume > VolumeEpsilon)
            {
                settings.StartupVolume = currentVolume;
            }
        });
    }
}
