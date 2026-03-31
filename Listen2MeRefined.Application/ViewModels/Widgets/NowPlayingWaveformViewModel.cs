using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Utils;
using Serilog;
using SkiaSharp;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class NowPlayingWaveformViewModel : ViewModelBase, IWaveformViewportAware
{
    private const int DefaultWaveFormWidth = 480;
    private const int DefaultWaveFormHeight = 70;
    private static readonly TimeSpan BitmapDisposeDelay = TimeSpan.FromMilliseconds(250);

    private readonly ILogger _logger;
    private readonly IWaveformRenderer _waveformRenderer;
    private readonly IWaveformViewportPolicy _waveformViewportPolicy;
    private readonly IWaveformResizeScheduler _waveformResizeScheduler;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly TimedTask _timedTask;
    private string? _currentTrackPath;

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

    public NowPlayingWaveformViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IWaveformRenderer waveformRenderer,
        IWaveformViewportPolicy waveformViewportPolicy,
        IWaveformResizeScheduler waveformResizeScheduler,
        IMusicPlayerController musicPlayerController,
        IUiDispatcher uiDispatcher,
        TimedTask timedTask) : base(errorHandler, logger, messenger)
    {
        _logger = logger;
        _waveformRenderer = waveformRenderer;
        _waveformViewportPolicy = waveformViewportPolicy;
        _waveformResizeScheduler = waveformResizeScheduler;
        _musicPlayerController = musicPlayerController;
        _uiDispatcher = uiDispatcher;
        _timedTask = timedTask;

        _logger.Debug("[NowPlayingWaveformViewModel] initialized");
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<CurrentSongChangedMessage>(OnCurrentSongChangedMessage);
        RegisterMessage<AppThemeChangedMessage>(OnAppThemeChangedMessage);

        _timedTask.Start(
            TimeSpan.FromMilliseconds(100),
            () => _ = _uiDispatcher.InvokeAsync(() => OnPropertyChanged(nameof(CurrentTime)), ct));

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

        _logger.Debug("[NowPlayingWaveformViewModel] Finished InitializeAsync");
    }

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
            _logger.Error(ex, "[NowPlayingWaveformViewModel] Failed to redraw waveform after resize.");
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

    private async void OnCurrentSongChangedMessage(CurrentSongChangedMessage message)
    {
        try
        {
            if (string.Equals(_currentTrackPath, message.Value.Path, StringComparison.OrdinalIgnoreCase))
            {
                // Same track notification (for example after queue-only operations) should not redraw waveform.
                return;
            }

            _currentTrackPath = message.Value.Path;
            await DrawPlaceholderLineAsync();

            if (!string.IsNullOrWhiteSpace(_currentTrackPath))
            {
                await DrawTrackWaveFormAsync(_currentTrackPath);
            }

            await _uiDispatcher.InvokeAsync(() => TotalTime = message.Value.Length.TotalMilliseconds);
        }
        catch (Exception e)
        {
            _logger.Error(e, "[NowPlayingWaveformViewModel] Failed to draw waveform.");
        }
    }

    private void OnAppThemeChangedMessage(AppThemeChangedMessage message)
    {
        _logger.Debug("[NowPlayingWaveformViewModel] Received AppThemeChangedMessage");
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
            _logger.Warning(ex, "[NowPlayingWaveformViewModel] Failed to dispose previous waveform bitmap.");
        }
    }
}
