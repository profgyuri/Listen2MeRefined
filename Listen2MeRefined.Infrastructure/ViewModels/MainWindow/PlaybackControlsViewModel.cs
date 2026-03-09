using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using SkiaSharp;

namespace Listen2MeRefined.Infrastructure.ViewModels.MainWindow;

public partial class PlaybackControlsViewModel :
    ViewModelBase,
    INotificationHandler<CurrentSongNotification>,
    INotificationHandler<AppThemeChangedNotification>,
    IWaveformViewportAware
{
    private const float VolumeEpsilon = 0.0001f;
    private const int DefaultWaveFormWidth = 480;
    private const int DefaultWaveFormHeight = 70;
    private const int MinimumWaveFormWidth = 64;
    private const int MinimumWaveFormHeight = 24;
    private const int ResizeNoiseThreshold = 2;
    private static readonly TimeSpan WaveformResizeDebounce = TimeSpan.FromMilliseconds(120);

    private readonly ILogger _logger;
    private readonly IWaveFormDrawer<SKBitmap> _waveFormDrawer;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly IPlaybackDefaultsService _playbackDefaultsService;
    private readonly TimedTask _timedTask;
    private readonly SemaphoreSlim _waveformRenderLock = new(1, 1);
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly TimeSpan _waveformResizeDebounce;
    private float _lastNonZeroVolume = 0.7f;
    private int _waveformResizeRequestId;
    private string? _currentTrackPath;
    private CancellationTokenSource? _waveformResizeCts;
    private Task _pendingWaveformRedrawTask = Task.CompletedTask;

    [ObservableProperty] private SKBitmap _waveForm = new(1, 1);
    [ObservableProperty] private int _waveFormWidth;
    [ObservableProperty] private int _waveFormHeight;
    [ObservableProperty] private double _totalTime;
    [ObservableProperty] private bool _arePlaybackButtonsEnabled = true;
    [ObservableProperty] private bool _isMuted;
    
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

            if (IsMuted && clampedValue > VolumeEpsilon)
            {
                SetMuted(false);
            }
            else if (!IsMuted && clampedValue <= VolumeEpsilon)
            {
                SetMuted(true);
            }

            _playbackDefaultsService.PersistPlaybackDefaults(clampedValue, IsMuted);
            OnPropertyChanged();
            OnPropertyChanged(nameof(VolumeIconKind));
        }
    }

    public string VolumeIconKind =>
        IsMuted || Volume <= VolumeEpsilon
            ? "VolumeOff"
            : Volume < 0.34f
                ? "VolumeLow"
                : Volume < 0.67f
                    ? "VolumeMedium"
                    : "VolumeHigh";

    public PlaybackControlsViewModel(
        ILogger logger,
        IWaveFormDrawer<SKBitmap> waveFormDrawer,
        IMusicPlayerController musicPlayerController,
        IPlaybackDefaultsService playbackDefaultsService,
        TimedTask timedTask,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        TimeSpan? waveformResizeDebounce = null)
    {
        _logger = logger;
        _waveFormDrawer = waveFormDrawer;
        _musicPlayerController = musicPlayerController;
        _playbackDefaultsService = playbackDefaultsService;
        _timedTask = timedTask;
        _delayAsync = delayAsync ?? Task.Delay;
        _waveformResizeDebounce = waveformResizeDebounce ?? WaveformResizeDebounce;

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

        if (WaveFormWidth <= 0 || WaveFormHeight <= 0)
        {
            WaveFormWidth = DefaultWaveFormWidth;
            WaveFormHeight = DefaultWaveFormHeight;
        }

        _waveFormDrawer.SetSize(WaveFormWidth, WaveFormHeight);
        if (string.IsNullOrWhiteSpace(_currentTrackPath))
        {
            await DrawPlaceholderLineAsync(ct);
        }
        else
        {
            await DrawTrackWaveFormAsync(_currentTrackPath, ct);
        }

        _logger.Debug("[PlayerControlsViewModel] Finished InitializeCoreAsync");
    }

    public void UpdateWaveformViewport(double availableWidth, double availableHeight)
    {
        var normalizedViewport = NormalizeViewport(availableWidth, availableHeight);
        if (normalizedViewport is null)
        {
            return;
        }

        var (width, height) = normalizedViewport.Value;
        if (HasNoMeaningfulViewportChange(width, height))
        {
            return;
        }

        WaveFormWidth = width;
        WaveFormHeight = height;
        _waveFormDrawer.SetSize(width, height);

        ScheduleWaveformRedraw();
    }

    internal Task WaitForPendingWaveformRedrawAsync() => _pendingWaveformRedrawTask;

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
        if (IsMuted)
        {
            var restoredVolume = _lastNonZeroVolume > VolumeEpsilon ? _lastNonZeroVolume : 0.7f;
            _musicPlayerController.Volume = restoredVolume;
            SetMuted(false);
            _playbackDefaultsService.PersistPlaybackDefaults(restoredVolume, isMuted: false);
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
        _playbackDefaultsService.PersistPlaybackDefaults(0f, isMuted: true);
        OnPropertyChanged(nameof(Volume));
        OnPropertyChanged(nameof(VolumeIconKind));
    }

    private static (int Width, int Height)? NormalizeViewport(double availableWidth, double availableHeight)
    {
        if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 0)
        {
            return null;
        }

        if (double.IsNaN(availableHeight) || double.IsInfinity(availableHeight) || availableHeight <= 0)
        {
            return null;
        }

        var width = Math.Max(MinimumWaveFormWidth, (int)Math.Round(availableWidth));
        var height = Math.Max(MinimumWaveFormHeight, (int)Math.Round(availableHeight));
        return (width, height);
    }

    private bool HasNoMeaningfulViewportChange(int width, int height)
    {
        return Math.Abs(WaveFormWidth - width) <= ResizeNoiseThreshold
            && Math.Abs(WaveFormHeight - height) <= ResizeNoiseThreshold;
    }

    private void ScheduleWaveformRedraw()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _waveformResizeCts, cancellationTokenSource);
        previous?.Cancel();
        previous?.Dispose();

        var requestId = Interlocked.Increment(ref _waveformResizeRequestId);
        _pendingWaveformRedrawTask = RedrawWaveformAfterDebounceAsync(requestId, cancellationTokenSource.Token);
    }

    private async Task RedrawWaveformAfterDebounceAsync(int requestId, CancellationToken cancellationToken)
    {
        try
        {
            await _delayAsync(_waveformResizeDebounce, cancellationToken);
            if (requestId != _waveformResizeRequestId || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var trackPath = _currentTrackPath;
            if (string.IsNullOrWhiteSpace(trackPath))
            {
                await DrawPlaceholderLineAsync(cancellationToken);
                return;
            }

            await DrawTrackWaveFormAsync(trackPath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore canceled resize redraws.
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[PlayerControlsViewModel] Failed to redraw waveform after resize.");
        }
    }

    private async Task DrawPlaceholderLineAsync(CancellationToken cancellationToken = default)
    {
        await _waveformRenderLock.WaitAsync(cancellationToken);
        try
        {
            WaveForm = await _waveFormDrawer.LineAsync();
        }
        finally
        {
            _waveformRenderLock.Release();
        }
    }

    private async Task DrawTrackWaveFormAsync(string trackPath, CancellationToken cancellationToken = default)
    {
        await _waveformRenderLock.WaitAsync(cancellationToken);
        try
        {
            WaveForm = await _waveFormDrawer.WaveFormAsync(trackPath);
        }
        finally
        {
            _waveformRenderLock.Release();
        }
    }

    public async Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[PlayerControlsViewModel] Received CurrentSongNotification: {@Audio}", notification.Audio);

        try
        {
            ArePlaybackButtonsEnabled = false;
            _currentTrackPath = notification.Audio.Path;
            await DrawPlaceholderLineAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(_currentTrackPath))
            {
                await DrawTrackWaveFormAsync(_currentTrackPath, cancellationToken);
            }

            TotalTime = notification.Audio.Length.TotalMilliseconds;
            OnPropertyChanged(nameof(TotalTimeDisplay));
        }
        catch (Exception e)
        {
            _logger.Error(e, "[PlayerControlsViewModel] Failed to draw waveform.");
        }
        finally
        {
            ArePlaybackButtonsEnabled = true;
        }
    }

    public Task Handle(AppThemeChangedNotification notification, CancellationToken cancellationToken)
    {
        ScheduleWaveformRedraw();
        return Task.CompletedTask;
    }

    private void SetMuted(bool isMuted)
    {
        if (IsMuted == isMuted)
        {
            return;
        }

        IsMuted = isMuted;
        OnPropertyChanged(nameof(VolumeIconKind));
    }

    private void ApplyStartupPlaybackDefaults()
    {
        var (startupVolume, startsMuted) = _playbackDefaultsService.LoadStartupDefaults();
        if (startupVolume > VolumeEpsilon)
        {
            _lastNonZeroVolume = startupVolume;
        }

        _musicPlayerController.Volume = startsMuted ? 0f : startupVolume;
        SetMuted(startsMuted || startupVolume <= VolumeEpsilon);
    }
}
