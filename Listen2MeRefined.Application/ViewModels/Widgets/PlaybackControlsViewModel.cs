using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Serilog;
using SkiaSharp;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class PlaybackControlsViewModel : ViewModelBase, IWaveformViewportAware
{
    private const float VolumeEpsilon = 0.0001f;
    private const int DefaultWaveFormWidth = 480;
    private const int DefaultWaveFormHeight = 70;
    private static readonly TimeSpan BitmapDisposeDelay = TimeSpan.FromMilliseconds(250);

    private readonly ILogger _logger;
    private readonly IWaveformRenderer _waveformRenderer;
    private readonly IWaveformViewportPolicy _waveformViewportPolicy;
    private readonly IWaveformResizeScheduler _waveformResizeScheduler;
    private readonly IPlaybackVolumeSetter _playbackVolumeSetter;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly TimedTask _timedTask;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IUiDispatcher _uiDispatcher;
    private string? _currentTrackPath;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
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
            var previousMutedState = IsMuted;
            var change = _playbackVolumeSetter.SetVolume(value);
            if (!change.HasVolumeChanged)
            {
                return;
            }

            if (previousMutedState != change.IsMuted)
            {
                SetMuted(change.IsMuted);
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(VolumeIconKind));
        }
    }

    public string VolumeIconKind => _playbackVolumeSetter.GetVolumeIconKind();

    public PlaybackControlsViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IWaveformRenderer waveformRenderer,
        IWaveformViewportPolicy waveformViewportPolicy,
        IWaveformResizeScheduler waveformResizeScheduler,
        IPlaybackVolumeSetter playbackVolumeSetter,
        IMusicPlayerController musicPlayerController,
        IAppSettingsReader settingsReader,
        IUiDispatcher uiDispatcher,
        TimedTask timedTask) : base(errorHandler, logger, messenger)
    {
        _logger = logger;
        _waveformRenderer = waveformRenderer;
        _waveformViewportPolicy = waveformViewportPolicy;
        _waveformResizeScheduler = waveformResizeScheduler;
        _playbackVolumeSetter = playbackVolumeSetter;
        _musicPlayerController = musicPlayerController;
        _settingsReader = settingsReader;
        _uiDispatcher = uiDispatcher;
        _timedTask = timedTask;

        _logger.Debug("[PlayerControlsViewModel] initialized");
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<CurrentSongChangedMessage>(OnCurrentSongChangedMessage);
        RegisterMessage<AppThemeChangedMessage>(OnAppThemeChangedMessage);
        
        FontFamilyName = _settingsReader.GetFontFamily();
        
        ApplyStartupPlaybackDefaults();

        _timedTask.Start(
            TimeSpan.FromMilliseconds(100),
            () =>
            {
                _ = _uiDispatcher.InvokeAsync(() =>
                {
                    OnPropertyChanged(nameof(CurrentTime));
                    OnPropertyChanged(nameof(CurrentTimeDisplay));
                }, ct);
            });

        OnPropertyChanged(nameof(Volume));
        SetMuted(Volume <= VolumeEpsilon);

        OnPropertyChanged(nameof(VolumeIconKind));
        OnPropertyChanged(nameof(IsMuted));
        OnPropertyChanged(nameof(TotalTimeDisplay));

        if (WaveFormWidth <= 0 || WaveFormHeight <= 0)
        {
            WaveFormWidth = DefaultWaveFormWidth;
            WaveFormHeight = DefaultWaveFormHeight;
        }

        _waveformRenderer.SetSize(WaveFormWidth, WaveFormHeight);
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

    /// <summary>
    /// Updates the waveform viewport dimensions and schedules a redraw when the change is meaningful.
    /// </summary>
    /// <param name="availableWidth">The available viewport width.</param>
    /// <param name="availableHeight">The available viewport height.</param>
    public void UpdateWaveformViewport(double availableWidth, double availableHeight)
    {
        var normalizedViewport = _waveformViewportPolicy.TryNormalizeViewport(availableWidth, availableHeight);
        if (normalizedViewport is null)
        {
            return;
        }

        var (width, height) = normalizedViewport.Value;
        if (!_waveformViewportPolicy.HasMeaningfulChange(WaveFormWidth, WaveFormHeight, width, height))
        {
            return;
        }

        WaveFormWidth = width;
        WaveFormHeight = height;
        _waveformRenderer.SetSize(width, height);

        ScheduleWaveformRedraw();
    }

    public Task WaitForPendingWaveformRedrawAsync() => _waveformResizeScheduler.PendingTask;

    [RelayCommand]
    private async Task PlayPause()
    {
        await ExecuteSafeAsync(async _ =>
        {
            _logger.Debug("[PlayerControlsViewModel] Toggling play/pause");
            await _musicPlayerController.PlayPauseAsync();
        });
    }

    [RelayCommand]
    private async Task Stop()
    {
        await ExecuteSafeAsync(_ =>
        {
            _logger.Debug("[PlayerControlsViewModel] Stopping playback");
            _musicPlayerController.Stop();
            
            return Task.CompletedTask;
        });
    }

    [RelayCommand]
    private async Task Next()
    {
        await ExecuteSafeAsync(async _ =>
        {
            _logger.Debug("[PlayerControlsViewModel] Skipping to next track");
            await _musicPlayerController.NextAsync();
        });
    }

    [RelayCommand]
    private async Task Previous()
    {
        await ExecuteSafeAsync(async _ =>
        {
            _logger.Debug("[PlayerControlsViewModel] Skipping to previous track");
            await _musicPlayerController.PreviousAsync();
        });
    }

    [RelayCommand]
    private async Task Shuffle()
    {
        await ExecuteSafeAsync(async _ =>
        {
            _logger.Debug("[PlayerControlsViewModel] Shuffling playlist");
            await _musicPlayerController.Shuffle();
        });
    }

    [RelayCommand]
    private async Task ToggleMute()
    {
        await ExecuteSafeAsync(_ =>
        {
            var change = _playbackVolumeSetter.ToggleMute();
            SetMuted(change.IsMuted);
            OnPropertyChanged(nameof(Volume));
            OnPropertyChanged(nameof(VolumeIconKind));
            
            return Task.CompletedTask;
        });
    }

    private void ScheduleWaveformRedraw()
    {
        _ = _waveformResizeScheduler.ScheduleResizeAsync(RedrawWaveformAsync);
    }

    private async Task RedrawWaveformAsync(CancellationToken cancellationToken)
    {
        try
        {
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
        var renderedBitmap = await _waveformRenderer.DrawPlaceholderAsync(cancellationToken);
        await UpdateWaveFormAsync(renderedBitmap, cancellationToken);
    }

    private async Task DrawTrackWaveFormAsync(string trackPath, CancellationToken cancellationToken = default)
    {
        var renderedBitmap = await _waveformRenderer.DrawTrackAsync(trackPath, cancellationToken);
        await UpdateWaveFormAsync(renderedBitmap, cancellationToken);
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
        var state = _playbackVolumeSetter.ApplyStartupDefaults();
        SetMuted(state.IsMuted);
    }
    
    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        _logger.Debug("[PlayerControlsViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
        FontFamilyName = message.Value;
    }

    private async void OnCurrentSongChangedMessage(CurrentSongChangedMessage message)
    {
        try
        {
            if (string.Equals(_currentTrackPath, message.Value.Path, StringComparison.OrdinalIgnoreCase))
            {
                // Same track notification (for example after queue-only operations) should not redraw waveform.
                return;
            }

            await _uiDispatcher.InvokeAsync(() => ArePlaybackButtonsEnabled = false);
            _currentTrackPath = message.Value.Path;
            await DrawPlaceholderLineAsync();

            if (!string.IsNullOrWhiteSpace(_currentTrackPath))
            {
                await DrawTrackWaveFormAsync(_currentTrackPath);
            }

            await _uiDispatcher.InvokeAsync(() =>
            {
                TotalTime = message.Value.Length.TotalMilliseconds;
                OnPropertyChanged(nameof(TotalTimeDisplay));
            });
        }
        catch (Exception e)
        {
            _logger.Error(e, "[PlayerControlsViewModel] Failed to draw waveform.");
        }
        finally
        {
            try
            {
                await _uiDispatcher.InvokeAsync(() => ArePlaybackButtonsEnabled = true);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "[PlayerControlsViewModel] Failed to re-enable playback controls on UI thread.");
            }
        }
    }

    private void OnAppThemeChangedMessage(AppThemeChangedMessage message)
    {
        _logger.Debug("[PlayerControlsViewModel] Received AppThemeChangedMessage");
        ScheduleWaveformRedraw();
    }

    private async Task UpdateWaveFormAsync(SKBitmap renderedBitmap, CancellationToken cancellationToken)
    {
        SKBitmap? previousBitmap = null;
        try
        {
            await _uiDispatcher.InvokeAsync(() =>
            {
                previousBitmap = WaveForm;
                WaveForm = renderedBitmap;
            }, cancellationToken);
        }
        catch
        {
            renderedBitmap.Dispose();
            throw;
        }

        if (previousBitmap is not null && !ReferenceEquals(previousBitmap, renderedBitmap))
        {
            _ = DisposeBitmapAfterDelayAsync(previousBitmap);
        }
    }

    private async Task DisposeBitmapAfterDelayAsync(SKBitmap bitmap)
    {
        try
        {
            await Task.Delay(BitmapDisposeDelay).ConfigureAwait(false);
            bitmap.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "[PlayerControlsViewModel] Failed to dispose previous waveform bitmap.");
        }
    }
}
