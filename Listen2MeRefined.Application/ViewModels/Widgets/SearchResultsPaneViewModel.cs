using System.Collections;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class SearchResultsPaneViewModel : ViewModelBase
{
    private readonly IPlaylistQueueState _playlistQueueState;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAudioSearchExecutionService _audioSearchExecutionService;
    private readonly ISearchResultsTransferService _searchResultsTransferService;
    private readonly IDefaultPlaylistService _defaultPlaylistService;
    private readonly IPlaybackQueueActionsService _playbackQueueActionsService;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly IFileScanner _fileScanner;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    private PlayerState _playerState = PlayerState.Stopped;
    
    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    
    public SongContextMenuViewModel SongContextMenuViewModel { get; }

    public SearchResultsPaneViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IPlaylistQueueState playlistQueueState,
        IAppSettingsReader settingsReader,
        IAudioSearchExecutionService audioSearchExecutionService,
        ISearchResultsTransferService searchResultsTransferService,
        IDefaultPlaylistService defaultPlaylistService,
        IPlaybackQueueActionsService playbackQueueActionsService,
        IMusicPlayerController musicPlayerController,
        IFileScanner fileScanner,
        SongContextMenuViewModel songContextMenuViewModel) : base(errorHandler, logger, messenger)
    {
        _playlistQueueState = playlistQueueState;
        _settingsReader = settingsReader;
        _audioSearchExecutionService = audioSearchExecutionService;
        _searchResultsTransferService = searchResultsTransferService;
        _defaultPlaylistService = defaultPlaylistService;
        _playbackQueueActionsService = playbackQueueActionsService;
        _musicPlayerController = musicPlayerController;
        _fileScanner = fileScanner;
        SongContextMenuViewModel = songContextMenuViewModel;
    }

    public bool GetIsDefaultPlaylistActive() => _playlistQueueState.IsDefaultPlaylistActive;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<QuickSearchExecutedMessage>(OnQuickSearchExecutedMessage);
        RegisterMessage<SearchResultsUpdatedMessage>(OnSearchResultsUpdatedMessage);
        RegisterMessage<AdvancedSearchRequestedMessage>(OnAdvancedSearchRequestedMessage);
        RegisterMessage<PlaylistContextMenuActionRequestedMessage>(OnPlaylistContextMenuActionRequestedMessage);
        RegisterMessage<PlayerStateChangedMessage>(m => _playerState = m.Value);
        
        FontFamilyName = _settingsReader.GetFontFamily();
        
        SongContextMenuViewModel.SetHost(this);
        await SongContextMenuViewModel.EnsureInitializedAsync(cancellationToken);
        
        Logger.Debug("[SearchResultsPaneViewModel] Finished InitializeCoreAsync");
        await base.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Transfers selected search results (or all results when none are selected) into the default playlist.
    /// </summary>
    [RelayCommand]
    private async Task SendSelectedToPlaylist()
    {
        await ExecuteSafeAsync(_ =>
        {
            var transferMode = _settingsReader.GetSearchResultsTransferMode();
            var decision = _searchResultsTransferService.ResolveTransfer(
                SearchResults,
                _selectedSearchResults,
                transferMode);

            Logger.Debug(
                "[SearchResultsPaneViewModel] Sending {AddCount} song(s) to playlist from {ResultCount} visible result(s)",
                decision.SongsToAdd.Count,
                SearchResults.Count);

            if (decision.SongsToAdd.Count > 0)
            {
                Messenger.Send(new SearchResultsToPlaylistRequestedMessage(decision.SongsToAdd));
            }

            ApplyTransferDecision(decision);
            return Task.CompletedTask;
        });
    }

    [RelayCommand]
    private async Task SearchResultsSelectionAdded(IList items)
    {
        await ExecuteSafeAsync(_ =>
        {
            var selectedSongs = items.Cast<AudioModel>().ToArray();
            foreach (var song in selectedSongs)
            {
                _selectedSearchResults.Add(song);
            }

            PublishSongContextSelectionChanged();
            
            return Task.CompletedTask;
        });
    }

    [RelayCommand]
    private async Task SearchResultsSelectionRemoved(IList items)
    {
        await ExecuteSafeAsync(_ =>
        {
            var selectedSongs = items.Cast<AudioModel>().ToArray();
            foreach (var song in selectedSongs)
            {
                _selectedSearchResults.Remove(song);
            }

            PublishSongContextSelectionChanged();

            return Task.CompletedTask;
        });
    }

    public IReadOnlyCollection<AudioModel> GetDirectSongContextSelection() => _selectedSearchResults.ToArray();

    public IReadOnlyCollection<AudioModel> GetFallbackSongContextSelection() => [];

    public int? GetSongContextActivePlaylistId() => _playlistQueueState.ActiveNamedPlaylistId;
    
    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        Logger.Debug("[SearchResultsPaneViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
        FontFamilyName = message.Value;
    }
    
    private void OnQuickSearchExecutedMessage(QuickSearchExecutedMessage message)
    {
        var result = message.Value.ToArray();

        Logger.Information("[SearchResultsPaneViewModel] Received quick search results with {Count} results", result.Length);
        if (result.Length > 0)
        {
            Logger.Verbose(
                "[SearchResultsPaneViewModel] First {Shown} results are: {@Results}",
                Math.Min(5, result.Length),
                result.Take(5));
        }

        ApplySearchResultsUpdate(result);
    }

    private void OnSearchResultsUpdatedMessage(SearchResultsUpdatedMessage message)
    {
        var result = message.Value.ToArray();
        Logger.Information("[SearchResultsPaneViewModel] Received external search results with {Count} results", result.Length);
        ApplySearchResultsUpdate(result);
    }

    private void OnAdvancedSearchRequestedMessage(AdvancedSearchRequestedMessage message)
    {
        _ = ExecuteSafeAsync(async _ =>
        {
            var payload = message.Value;
            Logger.Information(
                "[SearchResultsPaneViewModel] Performing advanced search with {@Filters} filters (MatchMode: {MatchMode})",
                payload.Filters,
                payload.MatchMode);
            var result = (
                await _audioSearchExecutionService.ExecuteAdvancedSearchAsync(payload.Filters, payload.MatchMode))
                .ToArray();

            Logger.Information("[SearchResultsPaneViewModel] Advanced search returned {Count} results", result.Length);
            if (result.Length > 0)
            {
                Logger.Verbose(
                    "[SearchResultsPaneViewModel] First {Shown} results are: {@Results}",
                    Math.Min(5, result.Length),
                    result.Take(5));
            }

            Messenger.Send(new SearchResultsUpdatedMessage(result));
            Messenger.Send(new AdvancedSearchCompletedMessage(result.Length));
        });
    }

    /// <summary>
    /// Applies a full search-result refresh and resets local selection state.
    /// </summary>
    /// <param name="results">The replacement result set to display.</param>
    private void ApplySearchResultsUpdate(IReadOnlyList<AudioModel> results)
    {
        SearchResults.Clear();
        SearchResults.AddRange(results);
        _selectedSearchResults.Clear();
        PublishSongContextSelectionChanged();
    }

    /// <summary>
    /// Applies transfer-side effects to the pane state after playlist dispatch.
    /// </summary>
    /// <param name="decision">The transfer decision produced by the transfer service.</param>
    private void ApplyTransferDecision(SearchResultsTransferDecision decision)
    {
        if (decision.SongsToRemove.Count > 0)
        {
            foreach (var song in decision.SongsToRemove)
            {
                SearchResults.Remove(song);
            }
        }

        if (decision.ClearSelection)
        {
            _selectedSearchResults.Clear();
        }

        PublishSongContextSelectionChanged();
    }

    private void OnPlaylistContextMenuActionRequestedMessage(PlaylistContextMenuActionRequestedMessage message)
    {
        var request = message.Value;
        if (!ReferenceEquals(request.SourceViewModel, this))
        {
            return;
        }

        _ = request.Action switch
        {
            PlaylistContextMenuAction.Rescan => ExecuteSafeAsync(_ => RescanSelectedSongsAsync()),
            PlaylistContextMenuAction.PlayNow => ExecuteSafeAsync(_ => PlayNowAsync()),
            PlaylistContextMenuAction.PlayAfterCurrent => ExecuteSafeAsync(_ => PlayAfterCurrentAsync()),
            PlaylistContextMenuAction.AddToDefaultPlaylist => ExecuteSafeAsync(_ => AddToDefaultPlaylistAsync()),
            _ => Task.CompletedTask
        };
    }

    private async Task RescanSelectedSongsAsync()
    {
        var songs = _selectedSearchResults.ToArray();
        foreach (var song in songs)
        {
            if (string.IsNullOrWhiteSpace(song.Path))
            {
                continue;
            }

            var updated = await _fileScanner.ScanAsync(song.Path);

            ReplaceSongInCollection(SearchResults, updated);
            ReplaceSongInCollection(_playlistQueueState.PlayList, updated);
            ReplaceSongInCollection(_playlistQueueState.DefaultPlaylist, updated);

            _selectedSearchResults.Remove(song);
            _selectedSearchResults.Add(updated);

            Messenger.Send(new SongMetadataUpdatedMessage(updated));
        }
    }

    private static void ReplaceSongInCollection(
        ObservableCollection<AudioModel> collection,
        AudioModel updated)
    {
        var index = collection.IndexOf(updated);
        if (index >= 0)
        {
            collection[index] = updated;
        }
    }

    private async Task PlayNowAsync()
    {
        if (!_playlistQueueState.IsDefaultPlaylistActive)
        {
            return;
        }

        var songs = _selectedSearchResults.ToArray();
        if (songs.Length == 0)
        {
            return;
        }

        var currentIndex = _playlistQueueState.CurrentSongIndex;
        var beforeCount = _playlistQueueState.PlayList.Count;

        _defaultPlaylistService.InsertAfterCurrentInDefaultPlaylist(songs);

        if (_playlistQueueState.PlayList.Count == beforeCount)
        {
            return;
        }

        var jumpIndex = currentIndex >= 0 ? currentIndex + 1 : 0;
        if (jumpIndex >= _playlistQueueState.PlayList.Count)
        {
            return;
        }

        _playlistQueueState.SelectedIndex = jumpIndex;
        _playlistQueueState.SelectedSong = _playlistQueueState.PlayList[jumpIndex];
        await _playbackQueueActionsService.JumpToSelectedSongAsync();

        if (_playerState != PlayerState.Playing)
        {
            await _musicPlayerController.PlayPauseAsync();
        }
    }

    private Task PlayAfterCurrentAsync()
    {
        if (!_playlistQueueState.IsDefaultPlaylistActive)
        {
            return Task.CompletedTask;
        }

        var songs = _selectedSearchResults.ToArray();
        if (songs.Length > 0)
        {
            _defaultPlaylistService.InsertAfterCurrentInDefaultPlaylist(songs);
        }

        return Task.CompletedTask;
    }

    private Task AddToDefaultPlaylistAsync()
    {
        var songs = _selectedSearchResults.ToArray();
        if (songs.Length > 0)
        {
            _defaultPlaylistService.AddSearchResultsToDefaultPlaylist(songs);
        }

        return Task.CompletedTask;
    }

    private void PublishSongContextSelectionChanged()
    {
        Messenger.Send(new SongContextMenuSelectionChangedMessage(this));
    }
}
