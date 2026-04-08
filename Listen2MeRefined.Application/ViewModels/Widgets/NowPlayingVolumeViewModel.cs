using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Playback;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class NowPlayingVolumeViewModel : ViewModelBase
{
    private const float VolumeEpsilon = 0.0001f;
    private const float VolumeStep = 0.05f;
    private const float VolumeWheelStep = 0.02f;

    private readonly ILogger _logger;
    private readonly IPlaybackVolumeSetter _playbackVolumeSetter;
    private readonly IMusicPlayerController _musicPlayerController;

    [ObservableProperty] private bool _isMuted;

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

    public NowPlayingVolumeViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IPlaybackVolumeSetter playbackVolumeSetter,
        IMusicPlayerController musicPlayerController) : base(errorHandler, logger, messenger)
    {
        _logger = logger;
        _playbackVolumeSetter = playbackVolumeSetter;
        _musicPlayerController = musicPlayerController;

        _logger.Debug("[NowPlayingVolumeViewModel] initialized");
    }

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ApplyStartupPlaybackDefaults();
        OnPropertyChanged(nameof(Volume));
        SetMuted(Volume <= VolumeEpsilon);
        OnPropertyChanged(nameof(VolumeIconKind));
        OnPropertyChanged(nameof(IsMuted));

        _logger.Debug("[NowPlayingVolumeViewModel] Finished InitializeAsync");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void VolumeUp() => Volume = Math.Min(1f, Volume + VolumeStep);

    [RelayCommand]
    private void VolumeDown() => Volume = Math.Max(0f, Volume - VolumeStep);

    public void AdjustVolumeByDelta(int delta)
    {
        var step = delta > 0 ? VolumeWheelStep : -VolumeWheelStep;
        Volume = Math.Clamp(Volume + step, 0f, 1f);
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
}
