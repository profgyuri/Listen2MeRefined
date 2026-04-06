using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class PlaylistPaneViewModel : ViewModelBase, ISongContextMenuHost
{
    private readonly IPlaylistQueueState _playlistQueueState;
    private readonly IPlaylistQueueRoutingService _playlistQueueRoutingService;
    private readonly IDefaultPlaylistService _defaultPlaylistService;
    private readonly IPlaybackQueueActionsService _playbackQueueActionsService;
    private readonly IExternalDropImportService _externalDropImportService;
    private readonly IPlaylistSelectionService _playlistSelectionService;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IPlaylistLibraryService _playlistLibraryService;
    private readonly IPlaybackContextSyncService _playbackContextSyncService;
    private readonly IExternalAudioOpenService _externalAudioOpenService;
    private readonly IExternalAudioOpenInbox _externalAudioOpenInbox;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly IFileScanner _fileScanner;
    private readonly IObservableCollectionUpdater _collectionUpdater;
    private readonly ISongSelectionTracker _selectionTracker;
    private readonly Dictionary<int, ObservableCollection<AudioModel>> _playlistCache = new();

    private int? _currentPlaylistId;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private string _activePlaylistName = "Default";
    [ObservableProperty] private ObservableCollection<AudioModel> _currentPlaylistSongs = [];
    [ObservableProperty] private bool _isCompactPlaylistView;

    public PlaylistSidebarViewModel PlaylistSidebarViewModel { get; }
    public SongContextMenuViewModel SongContextMenuViewModel { get; }
    public ObservableCollection<AudioModel> PlayList => _playlistQueueState.PlayList;

    public AudioModel? SelectedSong
    {
        get => _playlistQueueState.SelectedSong;
        set => _playlistQueueState.SelectedSong = value;
    }

    public int SelectedIndex
    {
        get => _playlistQueueState.SelectedIndex;
        set => _playlistQueueState.SelectedIndex = value;
    }

    public bool IsDefaultPlaylist => _currentPlaylistId is null;

    public PlaylistPaneViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IPlaylistQueueState playlistQueueState,
        IPlaylistQueueRoutingService playlistQueueRoutingService,
        IDefaultPlaylistService defaultPlaylistService,
        IPlaybackQueueActionsService playbackQueueActionsService,
        IExternalDropImportService externalDropImportService,
        IPlaylistSelectionService playlistSelectionService,
        IPlaylistLibraryService playlistLibraryService,
        IPlaybackContextSyncService playbackContextSyncService,
        IExternalAudioOpenService externalAudioOpenService,
        IExternalAudioOpenInbox externalAudioOpenInbox,
        IMusicPlayerController musicPlayerController,
        IFileScanner fileScanner,
        IObservableCollectionUpdater collectionUpdater,
        IAppSettingsReader settingsReader,
        PlaylistSidebarViewModel playlistSidebarViewModel,
        SongContextMenuViewModel songContextMenuViewModel) : base(errorHandler, logger, messenger)
    {
        _playlistQueueState = playlistQueueState;
        _playlistQueueRoutingService = playlistQueueRoutingService;
        _defaultPlaylistService = defaultPlaylistService;
        _playbackQueueActionsService = playbackQueueActionsService;
        _externalDropImportService = externalDropImportService;
        _playlistSelectionService = playlistSelectionService;
        _settingsReader = settingsReader;
        _playlistLibraryService = playlistLibraryService;
        _playbackContextSyncService = playbackContextSyncService;
        _externalAudioOpenService = externalAudioOpenService;
        _externalAudioOpenInbox = externalAudioOpenInbox;
        _musicPlayerController = musicPlayerController;
        _fileScanner = fileScanner;
        _collectionUpdater = collectionUpdater;
        _selectionTracker = new SongSelectionTracker(PublishSongContextSelectionChanged);
        PlaylistSidebarViewModel = playlistSidebarViewModel;
        SongContextMenuViewModel = songContextMenuViewModel;
        _playlistQueueState.PropertyChanged += PlaylistQueueStateOnPropertyChanged;
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<PlaylistViewModeChangedMessage>(OnPlaylistViewModeChangedMessage);
        RegisterMessage<PlaylistMembershipChangedMessage>(OnPlaylistMembershipChangedMessage);
        RegisterMessage<SearchResultsToPlaylistRequestedMessage>(OnSearchResultsToPlaylistRequestedMessage);
        RegisterMessage<CurrentSongChangedMessage>(OnCurrentSongChangedMessage);
        RegisterMessage<PlaylistShuffledMessage>(OnPlaylistShuffledMessage);
        RegisterMessage<PlaylistContextMenuActionRequestedMessage>(OnPlaylistContextMenuActionRequestedMessage);
        RegisterMessage<PlaylistSidebarSelectionChangedMessage>(OnPlaylistSidebarSelectionChanged);
        RegisterMessage<SongMetadataUpdatedMessage>(OnSongMetadataUpdatedMessage);
        RegisterMessage<ShuffleRequestedMessage>(OnShuffleRequested);
        RegisterMessage<ActivateViewedPlaylistMessage>(OnActivateViewedPlaylist);
        RegisterMessage<PlaylistDeletedMessage>(OnPlaylistDeleted);

        // Initialize sidebar first so playlists are loaded
        await PlaylistSidebarViewModel.EnsureInitializedAsync(ct);

        // Default playlist is selected by sidebar on init
        _currentPlaylistId = null;
        CurrentPlaylistSongs = _playlistQueueState.DefaultPlaylist;
        ActivePlaylistName = "Default";

        FontFamilyName = _settingsReader.GetFontFamily();
        IsCompactPlaylistView = _settingsReader.GetUseCompactPlaylistView();
        SongContextMenuViewModel.SetHost(this);
        await SongContextMenuViewModel.EnsureInitializedAsync(ct);

        _externalAudioOpenInbox.RegisterConsumer(OnExternalAudioPathsOpened, replayPending: true);
    }

    /// <summary>
    /// Removes selected songs from the active playlist, or clears it when there is no explicit selection.
    /// </summary>
    [RelayCommand]
    private Task RemoveSelectedFromActiveTab() =>
        ExecuteSafeAsync(async _ =>
        {
            var selectedSongs = _playlistSelectionService.ResolveSelectedSongs(
                _selectionTracker.SelectedSongs,
                CurrentPlaylistSongs,
                SelectedSong);

            if (selectedSongs.Length == 0)
            {
                if (IsDefaultPlaylist)
                {
                    _defaultPlaylistService.RemoveFromDefaultPlaylist(_playlistQueueState.DefaultPlaylist.ToArray());
                    _selectionTracker.Clear();
                    return;
                }

                if (_currentPlaylistId is null)
                {
                    return;
                }

                var existingPaths = CurrentPlaylistSongs
                    .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                    .Select(x => x.Path!)
                    .ToArray();
                CurrentPlaylistSongs.Clear();
                await _playlistLibraryService.RemoveSongsByPathAsync(_currentPlaylistId.Value, existingPaths);
                Messenger.Send(new PlaylistMembershipChangedMessage(_currentPlaylistId.Value));

                return;
            }

            if (IsDefaultPlaylist)
            {
                _defaultPlaylistService.RemoveFromDefaultPlaylist(selectedSongs);
                _selectionTracker.Clear();
                return;
            }

            if (_currentPlaylistId is null)
            {
                return;
            }

            await _playlistLibraryService.RemoveSongsByPathAsync(_currentPlaylistId.Value, selectedSongs.Select(x => x.Path));
            foreach (var song in selectedSongs)
            {
                CurrentPlaylistSongs.Remove(song);
            }

            if (_playlistQueueState.ActiveNamedPlaylistId == _currentPlaylistId)
            {
                _playlistQueueRoutingService.ActivateNamedPlaylistQueue(_currentPlaylistId.Value, CurrentPlaylistSongs);
            }

            _selectionTracker.Clear();
            Messenger.Send(new PlaylistMembershipChangedMessage(_currentPlaylistId.Value));
        });

    [RelayCommand(CanExecute = nameof(CanJumpToSelectedSong))]
    private Task JumpToSelectedSong() =>
        ExecuteSafeAsync(_ => _playbackQueueActionsService.JumpToSelectedSongAsync());

    [RelayCommand]
    private Task SetSelectedSongAsNext() =>
        ExecuteSafeAsync(_ =>
        {
            _playbackQueueActionsService.SetSelectedSongAsNext();
            SyncVisiblePlaylistOrderWithActiveQueue();
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task ScanSelectedSongs() =>
        ExecuteSafeAsync(async _ =>
        {
            var songs = _playlistSelectionService.ResolveSelectedSongs(
                _selectionTracker.SelectedSongs,
                CurrentPlaylistSongs,
                SelectedSong);

            foreach (var song in songs)
            {
                if (string.IsNullOrWhiteSpace(song.Path))
                {
                    continue;
                }

                var updated = await _fileScanner.ScanAsync(song.Path);

                _collectionUpdater.ReplaceIfPresent(CurrentPlaylistSongs, updated);
                _collectionUpdater.ReplaceIfPresent(_playlistQueueState.PlayList, updated);
                _collectionUpdater.ReplaceIfPresent(_playlistQueueState.DefaultPlaylist, updated);

                Messenger.Send(new SongMetadataUpdatedMessage(updated));
            }

            _selectionTracker.Clear();
        });

    /// <summary>
    /// Removes only the current selection from the default playlist.
    /// </summary>
    public Task RemoveSelectedFromDefaultPlaylistSelectionAsync() =>
        ExecuteSafeAsync(_ =>
        {
            if (!IsDefaultPlaylist)
            {
                return Task.CompletedTask;
            }

            var selectedSongs = _playlistSelectionService.ResolveSelectedSongs(
                _selectionTracker.SelectedSongs,
                CurrentPlaylistSongs,
                SelectedSong);

            if (selectedSongs.Length == 0)
            {
                return Task.CompletedTask;
            }

            _defaultPlaylistService.RemoveFromDefaultPlaylist(selectedSongs);
            _selectionTracker.Clear();
            return Task.CompletedTask;
        });

    /// <summary>
    /// Activates the current playlist as playback source and jumps to the currently selected song.
    /// </summary>
    [RelayCommand]
    private Task PlaySelectedFromActiveTab() =>
        ExecuteSafeAsync(async _ =>
        {
            if (SelectedSong is null)
            {
                return;
            }

            if (IsDefaultPlaylist)
            {
                _playlistQueueRoutingService.ActivateDefaultPlaylistQueue();
            }
            else if (_currentPlaylistId is not null)
            {
                _playlistQueueRoutingService.ActivateNamedPlaylistQueue(_currentPlaylistId.Value, CurrentPlaylistSongs);
            }

            var jumpIndex = IndexOfSongByPath(_playlistQueueState.PlayList, SelectedSong.Path);
            if (jumpIndex < 0)
            {
                return;
            }

            _playlistQueueState.SelectedIndex = jumpIndex;
            _playlistQueueState.SelectedSong = _playlistQueueState.PlayList[jumpIndex];
            await _playbackQueueActionsService.JumpToSelectedSongAsync();
        });

    [RelayCommand]
    private Task PlaylistSelectionAdded(IList items) =>
        ExecuteSafeAsync(_ =>
        {
            _selectionTracker.AddSelection(items);
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task PlaylistSelectionRemoved(IList items) =>
        ExecuteSafeAsync(_ =>
        {
            _selectionTracker.RemoveSelection(items);
            return Task.CompletedTask;
        });

    public IReadOnlyCollection<AudioModel> GetDirectSongContextSelection() => _selectionTracker.SelectedSongs;

    public IReadOnlyCollection<AudioModel> GetFallbackSongContextSelection() =>
        CurrentPlaylistSongs?.ToArray() ?? [];

    public AudioModel? GetFocusedSong() => SelectedSong;

    public int? GetSongContextActivePlaylistId() =>
        IsDefaultPlaylist ? null : _currentPlaylistId;

    bool ISongContextMenuHost.ShowPlaylistMembershipActions => true;

    bool ISongContextMenuHost.ShowRemoveFromPlaylistAction => IsDefaultPlaylist;

    bool ISongContextMenuHost.ShowAddToDefaultPlaylistAction => false;

    bool ISongContextMenuHost.ArePlaybackActionsAvailable => true;

    public Task HandleExternalFileDropAsync(IReadOnlyList<string> droppedPaths, int insertIndex, CancellationToken ct = default) =>
        _externalDropImportService.HandleExternalFileDropAsync(droppedPaths, insertIndex, ct);

    private void PlaylistQueueStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IPlaylistQueueState.SelectedSong))
        {
            OnPropertyChanged(nameof(SelectedSong));
            PublishSongContextSelectionChanged();
            JumpToSelectedSongCommand.NotifyCanExecuteChanged();
        }
        else if (e.PropertyName == nameof(IPlaylistQueueState.SelectedIndex))
        {
            OnPropertyChanged(nameof(SelectedIndex));
            JumpToSelectedSongCommand.NotifyCanExecuteChanged();
        }
    }

    private static int IndexOfSongByPath(IEnumerable<AudioModel> songs, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return -1;
        }

        var index = 0;
        foreach (var song in songs)
        {
            if (!string.IsNullOrWhiteSpace(song.Path) &&
                song.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    private void PublishSongContextSelectionChanged() =>
        Messenger.Send(new SongContextMenuSelectionChangedMessage(this));

    private bool CanJumpToSelectedSong()
    {
        return _playbackQueueActionsService.CanJumpToSelectedSong();
    }

    private async void OnPlaylistSidebarSelectionChanged(PlaylistSidebarSelectionChangedMessage message)
    {
        try
        {
            var playlistId = message.Value.PlaylistId;

            _selectionTracker.Clear();

            if (playlistId is null)
            {
                _currentPlaylistId = null;
                CurrentPlaylistSongs = _playlistQueueState.DefaultPlaylist;
                ActivePlaylistName = "Default";
                return;
            }

            _currentPlaylistId = playlistId;

            if (_playlistCache.TryGetValue(playlistId.Value, out var cached))
            {
                CurrentPlaylistSongs = cached;
            }
            else
            {
                var songs = await _playlistLibraryService.GetPlaylistSongsAsync(playlistId.Value);
                CurrentPlaylistSongs = new ObservableCollection<AudioModel>(songs);
                _playlistCache[playlistId.Value] = CurrentPlaylistSongs;
            }

            ActivePlaylistName = PlaylistSidebarViewModel.SelectedItem?.Name ?? "Playlist";
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to load playlist songs");
        }
    }

    private async void OnPlaylistMembershipChangedMessage(PlaylistMembershipChangedMessage message)
    {
        try
        {
            var playlistId = message.Value;
            _playlistCache.Remove(playlistId);

            if (_currentPlaylistId != playlistId)
            {
                return;
            }

            var songs = await _playlistLibraryService.GetPlaylistSongsAsync(playlistId);
            CurrentPlaylistSongs.Clear();
            CurrentPlaylistSongs.AddRange(songs);
            _playlistCache[playlistId] = CurrentPlaylistSongs;

            if (_playlistQueueState.ActiveNamedPlaylistId == playlistId)
            {
                _playlistQueueRoutingService.ActivateNamedPlaylistQueue(playlistId, CurrentPlaylistSongs);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to update playlist membership");
        }
    }

    private void OnPlaylistShuffledMessage(PlaylistShuffledMessage message)
    {
        try
        {
            SyncVisiblePlaylistOrderWithActiveQueue();
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to shuffle playlist");
        }
    }

    private void OnSongMetadataUpdatedMessage(SongMetadataUpdatedMessage message)
    {
        _collectionUpdater.ReplaceIfPresent(CurrentPlaylistSongs, message.Value);
    }

    private void SyncVisiblePlaylistOrderWithActiveQueue()
    {
        _playlistQueueRoutingService.SyncDefaultPlaylistOrder();

        if (IsDefaultPlaylist ||
            _playlistQueueState.ActiveNamedPlaylistId != _currentPlaylistId)
        {
            return;
        }

        var target = _playlistQueueState.PlayList;
        var songs = CurrentPlaylistSongs;
        for (var index = 0; index < target.Count; index++)
        {
            var currentPos = songs.IndexOf(target[index]);
            if (currentPos >= 0 && currentPos != index)
            {
                songs.Move(currentPos, index);
            }
        }
    }

    private void OnPlaylistContextMenuActionRequestedMessage(PlaylistContextMenuActionRequestedMessage message)
    {
        var request = message.Value;
        if (!ReferenceEquals(request.SourceHost, this))
        {
            return;
        }

        _ = request.Action switch
        {
            PlaylistContextMenuAction.Rescan => ScanSelectedSongs(),
            PlaylistContextMenuAction.PlayNow => PlaySelectedFromActiveTab(),
            PlaylistContextMenuAction.PlayAfterCurrent => SetSelectedSongAsNext(),
            PlaylistContextMenuAction.RemoveFromPlaylist => RemoveSelectedFromDefaultPlaylistSelectionAsync(),
            _ => Task.CompletedTask
        };
    }

    private async void OnShuffleRequested(ShuffleRequestedMessage message)
    {
        try
        {
            ShuffleCurrentPlaylistSongsInPlace();

            if (_musicPlayerController.HasCurrentSong)
            {
                return;
            }

            ActivateViewedPlaylistAsQueue();
            await _musicPlayerController.JumpToIndexAsync(0);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to handle shuffle request");
        }
    }

    private void OnActivateViewedPlaylist(ActivateViewedPlaylistMessage message)
    {
        try
        {
            ActivateViewedPlaylistAsQueue();
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to activate viewed playlist");
        }
    }

    private void OnPlaylistDeleted(PlaylistDeletedMessage message)
    {
        _playlistCache.Remove(message.Value.PlaylistId);
    }

    private void ActivateViewedPlaylistAsQueue()
    {
        if (IsDefaultPlaylist)
        {
            _playlistQueueRoutingService.ActivateDefaultPlaylistQueue();
        }
        else if (_currentPlaylistId is not null)
        {
            _playlistQueueRoutingService.ActivateNamedPlaylistQueue(_currentPlaylistId.Value, CurrentPlaylistSongs);
        }
    }

    private void ShuffleCurrentPlaylistSongsInPlace()
    {
        var songs = CurrentPlaylistSongs;
        var count = songs.Count;
        var rng = Random.Shared;

        for (var i = count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            if (i != j)
            {
                songs.Move(j, i);
            }
        }
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        FontFamilyName = message.Value;
    }

    private void OnPlaylistViewModeChangedMessage(PlaylistViewModeChangedMessage message)
    {
        IsCompactPlaylistView = message.Value;
    }

    private void OnSearchResultsToPlaylistRequestedMessage(SearchResultsToPlaylistRequestedMessage message)
    {
        _defaultPlaylistService.AddSearchResultsToDefaultPlaylist(message.Value);
    }

    private void OnCurrentSongChangedMessage(CurrentSongChangedMessage message)
    {
        Logger.Information("[PlaylistPaneViewModel] Current song changed to {@Audio}", message.Value);
        _externalAudioOpenService.SetCurrentSong(message.Value);
        _playbackContextSyncService.SetCurrentSong(message.Value);
    }

    private void OnExternalAudioPathsOpened(IReadOnlyList<string> paths)
    {
        Logger.Information(
            "[PlaylistPaneViewModel] Handling {Count} shell-opened audio file(s)",
            paths.Count);
        _ = ExecuteSafeAsync(ct => _externalAudioOpenService.OpenAsync(paths, ct));
    }
}
