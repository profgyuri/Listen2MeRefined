namespace Listen2MeRefined.Infrastructure.Mvvm.MainWindow;

using System.Collections;
using System.Collections.ObjectModel;

public partial class PlaylistPaneViewModel : ViewModelBase
{
    private readonly ListsViewModel _lists;

    public ObservableCollection<AudioModel> PlayList => _lists.PlayList;

    public AudioModel? SelectedSong
    {
        get => _lists.SelectedSong;
        set => _lists.SelectedSong = value;
    }

    public int SelectedIndex
    {
        get => _lists.SelectedIndex;
        set => _lists.SelectedIndex = value;
    }

    public IRelayCommand RemoveSelectedFromPlaylistCommand => _lists.RemoveSelectedFromPlaylistCommand;
    public IAsyncRelayCommand JumpToSelectedSongCommand => _lists.JumpToSelectedSongCommand;
    public IRelayCommand SwitchToSongMenuTabCommand => _lists.SwitchToSongMenuTabCommand;

    public PlaylistPaneViewModel(ListsViewModel lists)
    {
        _lists = lists;
    }

    [RelayCommand]
    private void PlaylistSelectionAdded(IList items) => _lists.AddSelectedPlaylistItems(items.Cast<AudioModel>());

    [RelayCommand]
    private void PlaylistSelectionRemoved(IList items) => _lists.RemoveSelectedPlaylistItems(items.Cast<AudioModel>());
}
