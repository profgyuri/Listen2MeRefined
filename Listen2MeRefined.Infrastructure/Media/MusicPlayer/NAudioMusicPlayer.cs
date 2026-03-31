using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

/// <summary>
/// Wrapper class for NAudio.
/// </summary>
public sealed partial class NAudioMusicPlayer : IMusicPlayerController
{
    private bool _startSongAutomatically;
    private int _outputDeviceIndex = -1;
    private AudioModel? _currentSong;
    private NAudio.Wave.WaveStream? _fileReader;
    private PlayerState _state = PlayerState.Stopped;
    private RepeatMode _repeatMode = RepeatMode.Off;

    private readonly ILogger _logger;
    private readonly IPlaybackQueueService _playbackQueueService;
    private readonly ITrackLoader _trackLoader;
    private readonly IPlaybackOutput _playbackOutput;
    private readonly IPlaybackProgressMonitor _playbackProgressMonitor;
    private readonly IMessenger _messenger;

    private const int TimeCheckInterval = 500;

    /// <summary>
    /// Gets or sets the current playback position in milliseconds.
    /// </summary>
    public double CurrentTime
    {
        get => _fileReader?.CurrentTime.TotalMilliseconds ?? 0;
        set
        {
            if (_fileReader is not null)
            {
                _fileReader.CurrentTime = TimeSpan.FromMilliseconds(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the playback output volume.
    /// </summary>
    public float Volume
    {
        get => _playbackOutput.Volume;
        set => _playbackOutput.Volume = value;
    }

    /// <summary>
    /// Gets or sets the repeat mode for auto-advancing at end of track or playlist.
    /// </summary>
    public RepeatMode RepeatMode
    {
        get => _repeatMode;
        set => _repeatMode = value;
    }

    /// <summary>
    /// Initializes a new player orchestrator with the required playback collaborators.
    /// </summary>
    public NAudioMusicPlayer(
        ILogger logger,
        TimedTask timedTask,
        IPlaybackQueueService playbackQueueService,
        ITrackLoader trackLoader,
        IPlaybackOutput playbackOutput,
        IPlaybackProgressMonitor playbackProgressMonitor,
        IAppSettingsReader settingsReader,
        IOutputDevice outputDevice,
        IMessenger messenger)
    {
        _logger = logger;
        _playbackQueueService = playbackQueueService;
        _trackLoader = trackLoader;
        _playbackOutput = playbackOutput;
        _playbackProgressMonitor = playbackProgressMonitor;
        _messenger = messenger;
        
        InitializeStartupOutputDevice(settingsReader, outputDevice);
        _messenger.Register<NAudioMusicPlayer, AudioOutputDeviceChangedMessage>(
            this,
            static (recipient, message) => recipient.OnAudioOutputDeviceChangedMessage(message));

        timedTask.Start(TimeSpan.FromMilliseconds(TimeCheckInterval), () => CheckPlaybackProgressAsync().GetAwaiter().GetResult());
        _logger.Debug("[NAudioMMusicPlayer] initialized");
    }

    /// <summary>
    /// Toggles playback between play and pause for the current track.
    /// </summary>
    public async Task PlayPauseAsync()
    {
        if (_currentSong is null)
        {
            await StartPlaybackAsync();
            return;
        }

        if (_state == PlayerState.Playing)
        {
            PausePlayback();
            return;
        }

        await StartPlaybackAsync();
    }

    /// <summary>
    /// Stops playback and seeks to the beginning of the current track.
    /// </summary>
    public void Stop()
    {
        _playbackOutput.Stop();
        if (_fileReader is not null)
        {
            _fileReader.CurrentTime = TimeSpan.Zero;
        }

        _startSongAutomatically = false;
        SetState(PlayerState.Stopped);
        _playbackProgressMonitor.Reset();
        _logger.Debug("[NAudioMMusicPlayer] Playback stopped by user");
    }

    /// <summary>
    /// Advances to the next track in the playback queue.
    /// </summary>
    public async Task NextAsync()
    {
        var nextTrack = _playbackQueueService.GetNextTrack();
        if (nextTrack is null)
        {
            _logger.Information("[NAudioMMusicPlayer] Playback is stopped, because the playlist is empty!");
            Stop();
            return;
        }

        await LoadSongAsync(nextTrack);
    }

    /// <summary>
    /// Moves to the previous track in the playback queue.
    /// </summary>
    public async Task PreviousAsync()
    {
        var previousTrack = _playbackQueueService.GetPreviousTrack();
        if (previousTrack is null)
        {
            _logger.Warning("[NAudioMMusicPlayer] Cannot go to the previous song, because the playlist is empty!");
            return;
        }

        await LoadSongAsync(previousTrack);
    }

    /// <summary>
    /// Jumps playback to the track at the provided queue index.
    /// </summary>
    /// <param name="index">The target track index.</param>
    public async Task JumpToIndexAsync(int index)
    {
        var track = _playbackQueueService.GetTrackAtIndex(index);
        if (track is null)
        {
            _logger.Warning("[NAudioMMusicPlayer] Cannot jump to song at index {Index}, because the playlist is empty!", index);
            return;
        }

        if (_state == PlayerState.Playing &&
            _currentSong is not null &&
            string.Equals(track.Path, _currentSong.Path, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Debug("[NAudioMMusicPlayer] Ignoring jump to already playing track at index {Index}", index);
            return;
        }

        await LoadSongAsync(track);
    }

    /// <summary>
    /// Shuffles the queue while keeping the current track consistent when available.
    /// </summary>
    public async Task Shuffle()
    {
        var shuffledCurrentTrack = _playbackQueueService.Shuffle(_currentSong);
        if (shuffledCurrentTrack is null)
        {
            _logger.Warning("[NAudioMMusicPlayer] Cannot shuffle an empty playlist!");
            return;
        }

        _messenger.Send(new PlaylistShuffledMessage());

        if (_currentSong is null)
        {
            await LoadSongAsync(shuffledCurrentTrack);
            return;
        }

        // Re-publish current track so shared queue state can refresh CurrentSongIndex after reordering.
        _messenger.Send(new CurrentSongChangedMessage(_currentSong));
    }

    internal async Task CheckPlaybackProgressAsync()
    {
        if (_fileReader is null)
        {
            return;
        }

        if (!_playbackProgressMonitor.ShouldAdvance(_fileReader.CurrentTime, _fileReader.TotalTime, _state == PlayerState.Playing))
        {
            return;
        }

        _logger.Debug("[NAudioMMusicPlayer] Current song reached its end, repeat mode: {RepeatMode}", _repeatMode);

        if (_repeatMode == RepeatMode.One)
        {
            _fileReader.CurrentTime = TimeSpan.Zero;
            _playbackProgressMonitor.Reset();
            _playbackOutput.Play();
            return;
        }

        if (_repeatMode == RepeatMode.Off && _playbackQueueService.IsAtLastTrack())
        {
            _logger.Information("[NAudioMMusicPlayer] End of playlist reached with repeat off, stopping.");
            Stop();
            return;
        }

        await NextAsync();
    }

    private async Task StartPlaybackAsync()
    {
        var currentTrack = _playbackQueueService.GetCurrentTrack();
        if (currentTrack is null)
        {
            _logger.Warning("[NAudioMMusicPlayer] Cannot start playback, because the playlist is empty!");
            return;
        }

        if (_currentSong is null)
        {
            _startSongAutomatically = true;
            await LoadSongAsync(currentTrack);
            return;
        }

        _startSongAutomatically = true;
        _playbackOutput.Play();
        SetState(PlayerState.Playing);
    }

    private void PausePlayback()
    {
        _playbackOutput.Pause();
        _startSongAutomatically = false;
        SetState(PlayerState.Paused);
    }

    private async Task LoadCurrentSongAsync()
    {
        var currentTrack = _playbackQueueService.GetCurrentTrack();
        if (currentTrack is null)
        {
            Stop();
            return;
        }

        await LoadSongAsync(currentTrack);
    }

    private async Task LoadSongAsync(AudioModel song)
    {
        var wasPlayingBeforeSwitch = _state == PlayerState.Playing;
        var shouldResumeAfterLoad = wasPlayingBeforeSwitch || _startSongAutomatically;
        var isTrackSwitch = _currentSong is null ||
                            !string.Equals(_currentSong.Path, song.Path, StringComparison.OrdinalIgnoreCase);

        if (isTrackSwitch && wasPlayingBeforeSwitch)
        {
            _playbackOutput.Pause();
            SetState(PlayerState.Paused);
        }

        _currentSong = song;

        var loadResult = _trackLoader.Load(song);
        if (!loadResult.IsSuccess)
        {
            await HandleUnplayableTrackAsync(song, loadResult);
            return;
        }

        _fileReader?.Dispose();
        _fileReader = loadResult.Reader;

        _messenger.Send(new CurrentSongChangedMessage(_currentSong));

        _playbackProgressMonitor.Reset();

        var reconfigured = ReconfigureOutput(shouldResumeAfterLoad, preservePosition: false);
        if (reconfigured && shouldResumeAfterLoad)
        {
            SetState(PlayerState.Playing);
        }
    }

    private bool ReconfigureOutput(bool resumePlayback, bool preservePosition)
    {
        try
        {
            if (_fileReader is null)
            {
                return false;
            }

            var timeStamp = _fileReader.CurrentTime;
            var result = _playbackOutput.Reinitialize(_fileReader, _outputDeviceIndex);
            if (!result.IsSuccess)
            {
                _logger.Warning(result.Exception, "[NAudioMMusicPlayer] Failed to reconfigure audio output: {Context}",
                    result.Context);
                if (!result.PreservedPreviousOutput)
                {
                    _startSongAutomatically = false;
                    SetState(PlayerState.Stopped);
                }

                return false;
            }

            if (preservePosition)
            {
                _fileReader.CurrentTime = timeStamp;
            }

            if (resumePlayback)
            {
                _playbackOutput.Play();
                SetState(PlayerState.Playing);
                _startSongAutomatically = true;
            }
            else if (_state == PlayerState.Paused)
            {
                _playbackOutput.Pause();
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, "[NAudioMMusicPlayer] Failed to reconfigure audio output");
            return false;
        }
    }

    private async Task HandleUnplayableTrackAsync(AudioModel track, TrackLoadResult result)
    {
        _logger.Warning("[NAudioMMusicPlayer] Skipping song {Path}. Status: {Status}. Reason: {Reason}", track.Path, result.Status, result.Reason);
        _playbackQueueService.RemoveTrack(track);
        await LoadCurrentSongAsync();
    }

    private void SetState(PlayerState newState)
    {
        _messenger.Send(new PlayerStateChangedMessage(newState));
        _state = newState;
    }

    private void OnAudioOutputDeviceChangedMessage(AudioOutputDeviceChangedMessage message)
    {
        try
        {
            ApplyAudioOutputDeviceAsync(message.Value);
        }
        catch (Exception e)
        {
            _logger.Error(e, "[NAudioMMusicPlayer] Failed to apply audio output device change");
        }
    }

    private void ApplyAudioOutputDeviceAsync(AudioOutputDevice device)
    {
        if (device.Index == _outputDeviceIndex)
        {
            return;
        }

        _outputDeviceIndex = device.Index;
        ReconfigureOutput(_state == PlayerState.Playing, preservePosition: true);
    }

    private void InitializeStartupOutputDevice(IAppSettingsReader settingsReader, IOutputDevice outputDevice)
    {
        try
        {
            var allDevices = outputDevice.EnumerateOutputDevices().ToArray();
            var selectedOutputDevice = allDevices
                .FirstOrDefault(x => x.Name == settingsReader.GetAudioOutputDeviceName())
                ?? allDevices.FirstOrDefault();

            if (selectedOutputDevice is null)
            {
                _logger.Warning("[NAudioMMusicPlayer] No audio output devices were detected on startup.");
                return;
            }

            _outputDeviceIndex = selectedOutputDevice.Index;
            _logger.Information(
                "[NAudioMMusicPlayer] Selected startup audio output device: {DeviceName}.",
                selectedOutputDevice.Name);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "[NAudioMMusicPlayer] Failed to resolve startup audio output device.");
        }
    }
}
