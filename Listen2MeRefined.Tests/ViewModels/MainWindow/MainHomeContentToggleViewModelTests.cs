using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Enums;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public sealed class MainHomeContentToggleViewModelTests
{
    [Fact]
    public async Task ShowPlaylistCommand_PublishesPlaylistToggleRequest()
    {
        var (viewModel, messenger) = await CreateViewModelAsync();
        MainHomeContentTarget? requested = null;
        var recipient = new object();

        messenger.Register<MainHomeContentToggleRequestedMessage>(recipient, (_, message) => requested = message.Value);

        viewModel.ShowPlaylistCommand.Execute(null);

        Assert.Equal(MainHomeContentTarget.Playlist, requested);
    }

    [Fact]
    public async Task ShowSearchResultsCommand_PublishesSearchResultsToggleRequest()
    {
        var (viewModel, messenger) = await CreateViewModelAsync();
        MainHomeContentTarget? requested = null;
        var recipient = new object();

        messenger.Register<MainHomeContentToggleRequestedMessage>(recipient, (_, message) => requested = message.Value);

        viewModel.ShowSearchResultsCommand.Execute(null);

        Assert.Equal(MainHomeContentTarget.SearchResults, requested);
    }

    [Fact]
    public async Task MainHomeContentActiveChangedMessage_UpdatesLocalSelectionState()
    {
        var (viewModel, messenger) = await CreateViewModelAsync();

        Assert.Equal(MainHomeContentTarget.Playlist, viewModel.ActiveTarget);
        Assert.True(viewModel.IsPlaylistActive);
        Assert.False(viewModel.IsSearchResultsActive);

        messenger.Send(new MainHomeContentActiveChangedMessage(MainHomeContentTarget.SearchResults));

        Assert.Equal(MainHomeContentTarget.SearchResults, viewModel.ActiveTarget);
        Assert.False(viewModel.IsPlaylistActive);
        Assert.True(viewModel.IsSearchResultsActive);
    }

    private static async Task<(MainHomeContentToggleViewModel ViewModel, WeakReferenceMessenger Messenger)> CreateViewModelAsync()
    {
        var messenger = new WeakReferenceMessenger();
        var viewModel = new MainHomeContentToggleViewModel(
            Mock.Of<IErrorHandler>(),
            Mock.Of<ILogger>(),
            messenger);

        await viewModel.InitializeAsync();
        return (viewModel, messenger);
    }
}
