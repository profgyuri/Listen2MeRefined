using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Listen2MeRefined.Infrastructure.Notifications;

namespace Listen2MeRefined.Infrastructure.ViewModels.MainWindow;

public partial class PlaylistPaneViewModel :
    ViewModelBase,
    INotificationHandler<PlaylistViewModeChangedNotification>
{
    private readonly ListsViewModel _lists;

    [ObservableProperty] private bool _isCompactPlaylistView;

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

    public PlaylistPaneViewModel(ListsViewModel lists, IAppSettingsReader settingsReader)
    {
        _lists = lists;
        IsCompactPlaylistView = settingsReader.GetUseCompactPlaylistView();
        _lists.PropertyChanged += ListsOnPropertyChanged;
    }

    [RelayCommand]
    private void PlaylistSelectionAdded(IList items) => _lists.AddSelectedPlaylistItems(items.Cast<AudioModel>());

    [RelayCommand]
    private void PlaylistSelectionRemoved(IList items) => _lists.RemoveSelectedPlaylistItems(items.Cast<AudioModel>());

    public Task Handle(PlaylistViewModeChangedNotification notification, CancellationToken cancellationToken)
    {
        IsCompactPlaylistView = notification.UseCompactPlaylistView;
        return Task.CompletedTask;
    }

    private void ListsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ListsViewModel.SelectedSong))
        {
            OnPropertyChanged(nameof(SelectedSong));
        }
        else if (e.PropertyName == nameof(ListsViewModel.SelectedIndex))
        {
            OnPropertyChanged(nameof(SelectedIndex));
        }
    }
}
