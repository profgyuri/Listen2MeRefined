using System.Collections;
using System.Collections.ObjectModel;

namespace Listen2MeRefined.Infrastructure.ViewModels.MainWindow;

public partial class SearchResultsPaneViewModel : ViewModelBase
{
    private readonly ListsViewModel _lists;

    public ObservableCollection<AudioModel> SearchResults => _lists.SearchResults;
    public IRelayCommand SendSelectedToPlaylistCommand => _lists.SendSelectedToPlaylistCommand;

    public SearchResultsPaneViewModel(ListsViewModel lists)
    {
        _lists = lists;
    }

    [RelayCommand]
    private void SearchResultsSelectionAdded(IList items) => _lists.AddSelectedSearchResults(items.Cast<AudioModel>());

    [RelayCommand]
    private void SearchResultsSelectionRemoved(IList items) => _lists.RemoveSelectedSearchResults(items.Cast<AudioModel>());
}
