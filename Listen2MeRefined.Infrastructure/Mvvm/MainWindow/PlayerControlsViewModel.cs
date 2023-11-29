namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using SkiaSharp;

public partial class PlayerControlsViewModel :
    ObservableObject,
    INotificationHandler<CurrentSongNotification>
{
    private readonly ILogger _logger;
    private readonly IWaveFormDrawer<SKBitmap> _waveFormDrawer;
    private readonly IMediaController _mediaController;
    private readonly TimedTask _timedTask;

    [ObservableProperty] private SKBitmap _waveForm = new(1, 1);
    [ObservableProperty] private int _waveFormWidth;
    [ObservableProperty] private int _waveFormHeight;
    [ObservableProperty] private double _totalTime;

    public double CurrentTime
    {
        get => _mediaController.CurrentTime;
        set
        {
            _mediaController.CurrentTime = value;
            OnPropertyChanged();
        }
    }

    public PlayerControlsViewModel(
        ILogger logger,
        IWaveFormDrawer<SKBitmap> waveFormDrawer,
        IMediaController mediaController,
        TimedTask timedTask)
    {
        _logger = logger;
        _waveFormDrawer = waveFormDrawer;
        _mediaController = mediaController;
        _timedTask = timedTask;
        _timedTask.Start(
            TimeSpan.FromMilliseconds(100),
            () => OnPropertyChanged(nameof(CurrentTime)));

        AsyncInit().ConfigureAwait(false);
    }

    private async Task AsyncInit()
    {
       await Task.Run(async () =>
        {
            WaveFormWidth = 480;
            WaveFormHeight = 70;
            _waveFormDrawer.SetSize(WaveFormWidth, WaveFormHeight);
            await DrawPlaceholderLineAsync();
        });
    }

    private async Task DrawPlaceholderLineAsync()
    {
        WaveForm = await _waveFormDrawer.LineAsync();
    }

    public async Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
    {
        await DrawPlaceholderLineAsync();
        WaveForm = await _waveFormDrawer.WaveFormAsync(notification.Audio.Path!);
        TotalTime = notification.Audio.Length.TotalMilliseconds;
    }

    #region Commands
    [RelayCommand]
    private async Task PlayPause()
    {
        await _mediaController.PlayPauseAsync();
    }

    [RelayCommand]
    private void Stop()
    {
        _mediaController.Stop();
    }

    [RelayCommand]
    private async Task Next()
    {
        await _mediaController.NextAsync();
    }

    [RelayCommand]
    private async Task Previous()
    {
        await _mediaController.PreviousAsync();
    }

    [RelayCommand]
    private async Task Shuffle()
    {
        await _mediaController.Shuffle();
    }
    #endregion
}