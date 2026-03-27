using System.Collections;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Core.Models;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class SearchResultsPaneViewModel : ViewModelBase
{
    private readonly IPlaylistQueueState _playlistQueueState;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAudioSearchExecutionService _audioSearchExecutionService;
    private readonly ISearchResultsTransferService _searchResultsTransferService;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    
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
        SongContextMenuViewModel songContextMenuViewModel) : base(errorHandler, logger, messenger)
    {
        _playlistQueueState = playlistQueueState;
        _settingsReader = settingsReader;
        _audioSearchExecutionService = audioSearchExecutionService;
        _searchResultsTransferService = searchResultsTransferService;
        SongContextMenuViewModel = songContextMenuViewModel;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<QuickSearchExecutedMessage>(OnQuickSearchExecutedMessage);
        RegisterMessage<SearchResultsUpdatedMessage>(OnSearchResultsUpdatedMessage);
        RegisterMessage<AdvancedSearchRequestedMessage>(OnAdvancedSearchRequestedMessage);
        
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

    private void PublishSongContextSelectionChanged()
    {
        Messenger.Send(new SongContextMenuSelectionChangedMessage(this));
    }
}
