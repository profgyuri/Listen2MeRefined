using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Core.Enums;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Widgets;

public partial class MainHomeContentToggleViewModel : ViewModelBase
{
    [ObservableProperty] private MainHomeContentTarget _activeTarget = MainHomeContentTarget.Playlist;

    public bool IsPlaylistActive => ActiveTarget == MainHomeContentTarget.Playlist;

    public bool IsSearchResultsActive => ActiveTarget == MainHomeContentTarget.SearchResults;

    public MainHomeContentToggleViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger) : base(errorHandler, logger, messenger)
    {
    }

    public override Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RegisterMessage<MainHomeContentActiveChangedMessage>(OnMainHomeContentActiveChangedMessage);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ShowPlaylist()
    {
        Messenger.Send(new MainHomeContentToggleRequestedMessage(MainHomeContentTarget.Playlist));
    }

    [RelayCommand]
    private void ShowSearchResults()
    {
        Messenger.Send(new MainHomeContentToggleRequestedMessage(MainHomeContentTarget.SearchResults));
    }

    partial void OnActiveTargetChanged(MainHomeContentTarget value)
    {
        OnPropertyChanged(nameof(IsPlaylistActive));
        OnPropertyChanged(nameof(IsSearchResultsActive));
    }

    private void OnMainHomeContentActiveChangedMessage(MainHomeContentActiveChangedMessage message)
    {
        ActiveTarget = message.Value;
    }
}
