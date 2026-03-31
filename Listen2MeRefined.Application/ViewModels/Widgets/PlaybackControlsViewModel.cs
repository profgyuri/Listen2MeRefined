using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Enums;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class PlaybackControlsViewModel : ViewModelBase
{
    private const float VolumeEpsilon = 0.0001f;

    private readonly ILogger _logger;
    private readonly IPlaybackVolumeSetter _playbackVolumeSetter;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly TimedTask _timedTask;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IUiDispatcher _uiDispatcher;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
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

    public RepeatMode RepeatMode => _musicPlayerController.RepeatMode;

    public string RepeatIconKind => _musicPlayerController.RepeatMode == RepeatMode.One
        ? "RepeatOnce"
        : "Repeat";

    public bool IsRepeatActive => _musicPlayerController.RepeatMode != RepeatMode.Off;

    public PlaybackControlsViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IPlaybackVolumeSetter playbackVolumeSetter,
        IMusicPlayerController musicPlayerController,
        IAppSettingsReader settingsReader,
        IUiDispatcher uiDispatcher,
        TimedTask timedTask) : base(errorHandler, logger, messenger)
    {
        _logger = logger;
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

        _logger.Debug("[PlayerControlsViewModel] Finished InitializeCoreAsync");

        await Task.CompletedTask;
    }

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
    private void ToggleRepeat()
    {
        _musicPlayerController.RepeatMode = _musicPlayerController.RepeatMode switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            RepeatMode.One => RepeatMode.Off,
            _ => RepeatMode.Off
        };
        OnPropertyChanged(nameof(RepeatMode));
        OnPropertyChanged(nameof(RepeatIconKind));
        OnPropertyChanged(nameof(IsRepeatActive));
        _logger.Debug("[PlayerControlsViewModel] Repeat mode changed to: {Mode}", _musicPlayerController.RepeatMode);
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

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        _logger.Debug("[PlayerControlsViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
        FontFamilyName = message.Value;
    }

    private async void OnCurrentSongChangedMessage(CurrentSongChangedMessage message)
    {
        try
        {
            await _uiDispatcher.InvokeAsync(() =>
            {
                ArePlaybackButtonsEnabled = true;
                TotalTime = message.Value.Length.TotalMilliseconds;
                OnPropertyChanged(nameof(TotalTimeDisplay));
            });
        }
        catch (Exception e)
        {
            _logger.Error(e, "[PlayerControlsViewModel] Failed to update total time.");
        }
    }
}
