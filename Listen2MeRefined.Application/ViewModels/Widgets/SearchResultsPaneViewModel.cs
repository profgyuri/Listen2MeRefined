using System.Collections;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.ContextMenus;
using Listen2MeRefined.Core.Models;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class SearchResultsPaneViewModel : ViewModelBase
{
    private readonly ListsViewModel _lists;
    private readonly HashSet<AudioModel> _selectedSearchResults = new();
    
    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    
    public SongContextMenuViewModel SongContextMenuViewModel { get; }
    public IRelayCommand SendSelectedToPlaylistCommand => _lists.SendSelectedToPlaylistCommand;

    public SearchResultsPaneViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        ListsViewModel lists,
        SongContextMenuViewModel songContextMenuViewModel) : base(errorHandler, logger, messenger)
    {
        _lists = lists;
        SongContextMenuViewModel = songContextMenuViewModel;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<QuickSearchExecutedMessage>(OnQuickSearchExecutedMessage);
        
        SongContextMenuViewModel.SetHost(this);
        await SongContextMenuViewModel.EnsureInitializedAsync(cancellationToken);
        
        Logger.Debug("[SearchResultsPaneViewModel] Finished InitializeCoreAsync");
        await base.InitializeAsync(cancellationToken);
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

            _lists.AddSelectedSearchResults(selectedSongs);
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

            _lists.RemoveSelectedSearchResults(selectedSongs);
            PublishSongContextSelectionChanged();

            return Task.CompletedTask;
        });
    }

    public IReadOnlyCollection<AudioModel> GetDirectSongContextSelection() => _selectedSearchResults.ToArray();

    public IReadOnlyCollection<AudioModel> GetFallbackSongContextSelection() => _lists.GetSelectedSearchResults();

    public int? GetSongContextActivePlaylistId() => _lists.ActiveNamedPlaylistId;
    
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
        PublishSongContextSelectionChanged();
    }

    private void PublishSongContextSelectionChanged()
    {
        Messenger.Send(new SongContextMenuSelectionChangedMessage(this));
    }
}
