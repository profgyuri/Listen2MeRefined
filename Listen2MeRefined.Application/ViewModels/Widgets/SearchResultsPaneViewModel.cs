using System.Collections;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Models;
using MediatR;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class SearchResultsPaneViewModel : ViewModelBase
{
    private readonly ListsViewModel _lists;
    private readonly IPlaylistLibraryService _playlistLibraryService;
    private readonly IMediator _mediator;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    
    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    
    public IRelayCommand SendSelectedToPlaylistCommand => _lists.SendSelectedToPlaylistCommand;

    public SearchResultsPaneViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        ListsViewModel lists,
        IPlaylistLibraryService playlistLibraryService,
        IMediator mediator) : base(errorHandler, logger, messenger)
    {
        _lists = lists;
        _playlistLibraryService = playlistLibraryService;
        _mediator = mediator;
    }

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<QuickSearchExecutedMessage>(OnQuickSearchExecutedMessage);
        
        return base.InitializeAsync(cancellationToken);
    }

    [RelayCommand]
    private void SearchResultsSelectionAdded(IList items)
    {
        var selectedSongs = items.Cast<AudioModel>().ToArray();
        foreach (var song in selectedSongs)
        {
            _selectedSearchResults.Add(song);
        }

        _lists.AddSelectedSearchResults(selectedSongs);
    }

    [RelayCommand]
    private void SearchResultsSelectionRemoved(IList items)
    {
        var selectedSongs = items.Cast<AudioModel>().ToArray();
        foreach (var song in selectedSongs)
        {
            _selectedSearchResults.Remove(song);
        }

        _lists.RemoveSelectedSearchResults(selectedSongs);
    }

    public async Task<IReadOnlyList<PlaylistPaneViewModel.PlaylistMenuState>> GetSongContextMenuPlaylistsAsync()
    {
        var selectedSongs = GetSelectedSongsForContext();
        if (selectedSongs.Length == 0)
        {
            return Array.Empty<PlaylistPaneViewModel.PlaylistMenuState>();
        }

        if (selectedSongs.Length == 1 && !string.IsNullOrWhiteSpace(selectedSongs[0].Path))
        {
            var singleMembership = await _playlistLibraryService.GetMembershipBySongPathAsync(selectedSongs[0].Path!);
            return singleMembership
                .Select(x => new PlaylistPaneViewModel.PlaylistMenuState(x.PlaylistId, x.PlaylistName, x.ContainsSong, AllowRemove: true))
                .ToArray();
        }

        var playlists = await _playlistLibraryService.GetAllPlaylistsAsync();
        return playlists
            .Select(x =>
            {
                var isCurrentNamed = _lists.ActiveNamedPlaylistId == x.Id;
                return new PlaylistPaneViewModel.PlaylistMenuState(x.Id, x.Name, isCurrentNamed, AllowRemove: isCurrentNamed);
            })
            .ToArray();
    }

    public async Task TogglePlaylistMembershipAsync(int playlistId, bool shouldContain, bool allowRemove)
    {
        var selectedSongs = GetSelectedSongsForContext();
        if (selectedSongs.Length == 0)
        {
            return;
        }

        var paths = selectedSongs.Select(x => x.Path).ToArray();
        if (shouldContain)
        {
            await _playlistLibraryService.AddSongsByPathAsync(playlistId, paths);
        }
        else
        {
            if (!allowRemove)
            {
                return;
            }

            await _playlistLibraryService.RemoveSongsByPathAsync(playlistId, paths);
        }

        await _mediator.Publish(new PlaylistMembershipChangedNotification(playlistId));
    }

    public async Task AddToNewPlaylistFromContextAsync(string name)
    {
        var selectedSongs = GetSelectedSongsForContext();
        if (selectedSongs.Length == 0)
        {
            return;
        }

        var created = await _playlistLibraryService.CreatePlaylistAsync(name);
        await _playlistLibraryService.AddSongsByPathAsync(created.Id, selectedSongs.Select(x => x.Path));

        await _mediator.Publish(new PlaylistCreatedNotification(created.Id, created.Name));
        await _mediator.Publish(new PlaylistMembershipChangedNotification(created.Id));
    }

    private AudioModel[] GetSelectedSongsForContext()
    {
        if (_selectedSearchResults.Count > 0)
        {
            return _selectedSearchResults
                .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                .Distinct()
                .ToArray();
        }

        return _lists.GetSelectedSearchResults()
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .Distinct()
            .ToArray();
    }
    
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

        SearchResults.Clear();
        SearchResults.AddRange(message.Value);
    }
}
