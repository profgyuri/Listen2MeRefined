using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Mvvm;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using MediatR;
using Moq;
using Serilog;
using ListsViewModel = Listen2MeRefined.Infrastructure.Mvvm.MainWindow.ListsViewModel;

namespace Listen2MeRefined.Tests.Mvvm;

public class ListsViewModelTests
{
    [Fact]
    public void SendSelectedToPlaylist_MovesOnlySelectedSearchResults()
    {
        var vm = CreateViewModel(out _);
        var first = new AudioModel { Title = "First", Path = "a" };
        var second = new AudioModel { Title = "Second", Path = "b" };

        vm.SearchResults.Add(first);
        vm.SearchResults.Add(second);
        vm.AddSelectedSearchResults([first]);

        vm.SendSelectedToPlaylistCommand.Execute(null);

        Assert.Single(vm.PlayList);
        Assert.Contains(first, vm.PlayList);
        Assert.DoesNotContain(first, vm.SearchResults);
        Assert.Contains(second, vm.SearchResults);
    }

    [Fact]
    public void RemoveSelectedFromPlaylist_RemovesOnlySelectedItems()
    {
        var vm = CreateViewModel(out _);
        var first = new AudioModel { Title = "First", Path = "a" };
        var second = new AudioModel { Title = "Second", Path = "b" };

        vm.PlayList.Add(first);
        vm.PlayList.Add(second);
        vm.AddSelectedPlaylistItems([first]);

        vm.RemoveSelectedFromPlaylistCommand.Execute(null);

        Assert.Single(vm.PlayList);
        Assert.DoesNotContain(first, vm.PlayList);
        Assert.Contains(second, vm.PlayList);
    }

    [Fact]
    public void SwitchTabCommands_ToggleVisibleTabFlags()
    {
        var vm = CreateViewModel(out _);

        vm.SwitchToSongMenuTabCommand.Execute(null);
        Assert.True(vm.IsSongMenuTabVisible);
        Assert.False(vm.IsSearchResultsTabVisible);

        vm.SwitchToSearchResultsTabCommand.Execute(null);
        Assert.True(vm.IsSearchResultsTabVisible);
        Assert.False(vm.IsSongMenuTabVisible);
    }

    [Fact]
    public async Task JumpToSelectedSongCommand_RequiresSelectedIndex()
    {
        var vm = CreateViewModel(out var playerController);

        Assert.False(vm.JumpToSelectedSongCommand.CanExecute(null));

        vm.SelectedIndex = 2;
        Assert.True(vm.JumpToSelectedSongCommand.CanExecute(null));

        await vm.JumpToSelectedSongCommand.ExecuteAsync(null);
        playerController.Verify(x => x.JumpToIndexAsync(2), Times.Once);
    }

    [Fact]
    public async Task HandleAdvancedSearchNotification_UsesMatchModeAndPublishesCompletion()
    {
        var logger = new Mock<ILogger>();
        var mediator = new Mock<IMediator>();
        var advancedReader = new Mock<IAdvancedDataReader<AdvancedFilter, AudioModel>>();
        var scanner = new Mock<IFileScanner>();
        var playerController = new Mock<IMusicPlayerController>();
        var playlist = new Playlist();

        var vm = new ListsViewModel(
            logger.Object,
            mediator.Object,
            advancedReader.Object,
            scanner.Object,
            playerController.Object,
            playlist);

        bool? capturedMatchAll = null;
        advancedReader
            .Setup(x => x.ReadAsync(It.IsAny<IEnumerable<AdvancedFilter>>(), It.IsAny<bool>()))
            .Callback<IEnumerable<AdvancedFilter>, bool>((_, matchAll) => capturedMatchAll = matchAll)
            .ReturnsAsync([]);
        mediator
            .Setup(x => x.Publish(It.IsAny<AdvancedSearchCompletedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await vm.Handle(
            new AdvancedSearchNotification(
                [new AdvancedFilter(nameof(AudioModel.Title), AdvancedFilterOperator.Contains, "rock")],
                SearchMatchMode.Any),
            CancellationToken.None);

        Assert.False(capturedMatchAll);
        mediator.Verify(
            x => x.Publish(It.Is<AdvancedSearchCompletedNotification>(n => n.ResultCount == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ListsViewModel CreateViewModel(out Mock<IMusicPlayerController> playerController)
    {
        var logger = new Mock<ILogger>();
        var mediator = new Mock<IMediator>();
        var advancedReader = new Mock<IAdvancedDataReader<AdvancedFilter, AudioModel>>();
        var scanner = new Mock<IFileScanner>();
        playerController = new Mock<IMusicPlayerController>();
        var playlist = new Playlist();

        return new ListsViewModel(
            logger.Object,
            mediator.Object,
            advancedReader.Object,
            scanner.Object,
            playerController.Object,
            playlist);
    }
}
