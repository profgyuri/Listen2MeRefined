using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Playlist;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels.MainWindow;

public class PlaylistSidebarViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsDefaultAndManualPlaylists()
    {
        var (vm, _, _, _) = CreateViewModel([
            new PlaylistSummary(1, "Favorites", true, 0),
            new PlaylistSummary(2, "Workout", false, 1)
        ]);

        await vm.InitializeAsync();

        Assert.NotNull(vm.DefaultPlaylist);
        Assert.True(vm.DefaultPlaylist.IsDefault);
        Assert.Equal(2, vm.ManualPlaylists.Count);
        Assert.Equal("Favorites", vm.ManualPlaylists[0].Name);
        Assert.Equal("Workout", vm.ManualPlaylists[1].Name);
        Assert.True(vm.ManualPlaylists[0].IsPinned);
    }

    [Fact]
    public async Task SelectPlaylist_SendsSelectionMessage()
    {
        var (vm, messenger, _, _) = CreateViewModel([]);
        await vm.InitializeAsync();

        PlaylistSidebarSelectionData? received = null;
        messenger.Register<PlaylistSidebarSelectionChangedMessage>(this, (_, msg) => received = msg.Value);

        vm.SelectPlaylistCommand.Execute(vm.DefaultPlaylist);

        Assert.NotNull(received);
        Assert.Null(received!.PlaylistId);
    }

    [Fact]
    public async Task SelectPlaylist_NamedPlaylist_SendsPlaylistId()
    {
        var (vm, messenger, _, _) = CreateViewModel([
            new PlaylistSummary(42, "Road Trip", false, 0)
        ]);
        await vm.InitializeAsync();

        PlaylistSidebarSelectionData? received = null;
        messenger.Register<PlaylistSidebarSelectionChangedMessage>(this, (_, msg) => received = msg.Value);

        vm.SelectPlaylistCommand.Execute(vm.ManualPlaylists[0]);

        Assert.NotNull(received);
        Assert.Equal(42, received!.PlaylistId);
        Assert.True(vm.ManualPlaylists[0].IsSelected);
    }

    [Fact]
    public async Task CreatePlaylist_AddsAndEntersRenameMode()
    {
        var (vm, _, playlistLibrary, _) = CreateViewModel([]);
        await vm.InitializeAsync();

        await vm.CreatePlaylistCommand.ExecuteAsync(null);

        Assert.Single(vm.ManualPlaylists);
        Assert.Equal("New Playlist", vm.ManualPlaylists[0].Name);
        Assert.True(vm.ManualPlaylists[0].IsRenaming);
        Assert.Equal(vm.ManualPlaylists[0], vm.SelectedItem);
    }

    [Fact]
    public async Task CommitRename_ValidatesAndCallsService()
    {
        var (vm, _, playlistLibrary, _) = CreateViewModel([
            new PlaylistSummary(10, "Old Name", false, 0)
        ]);
        await vm.InitializeAsync();

        var item = vm.ManualPlaylists[0];
        vm.BeginRenameCommand.Execute(item);
        Assert.True(item.IsRenaming);

        item.Name = "New Name";
        await vm.CommitRenameCommand.ExecuteAsync(item);

        Assert.False(item.IsRenaming);
        Assert.Equal("New Name", item.Name);
        playlistLibrary.Verify(x => x.RenamePlaylistAsync(10, "New Name", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitRename_TooShort_CancelsRename()
    {
        var (vm, _, _, _) = CreateViewModel([
            new PlaylistSummary(10, "Original", false, 0)
        ]);
        await vm.InitializeAsync();

        var item = vm.ManualPlaylists[0];
        vm.BeginRenameCommand.Execute(item);
        item.Name = "X";
        await vm.CommitRenameCommand.ExecuteAsync(item);

        Assert.False(item.IsRenaming);
        Assert.Equal("Original", item.Name);
    }

    [Fact]
    public async Task CancelRename_RevertsName()
    {
        var (vm, _, _, _) = CreateViewModel([
            new PlaylistSummary(10, "Original", false, 0)
        ]);
        await vm.InitializeAsync();

        var item = vm.ManualPlaylists[0];
        vm.BeginRenameCommand.Execute(item);
        item.Name = "Changed";
        vm.CancelRenameCommand.Execute(item);

        Assert.False(item.IsRenaming);
        Assert.Equal("Original", item.Name);
    }

    [Fact]
    public async Task TogglePin_CallsServiceAndResorts()
    {
        var (vm, _, playlistLibrary, _) = CreateViewModel([
            new PlaylistSummary(1, "Unpinned", false, 0),
            new PlaylistSummary(2, "Another", false, 1)
        ]);
        await vm.InitializeAsync();

        var itemToPin = vm.ManualPlaylists[1];
        await vm.TogglePinCommand.ExecuteAsync(itemToPin);

        Assert.True(itemToPin.IsPinned);
        playlistLibrary.Verify(x => x.SetPinnedAsync(2, true, It.IsAny<CancellationToken>()), Times.Once);
        // Pinned item should be sorted to the top
        Assert.Equal("Another", vm.ManualPlaylists[0].Name);
    }

    [Fact]
    public async Task DeletePlaylist_RemovesFromList()
    {
        var (vm, _, playlistLibrary, _) = CreateViewModel([
            new PlaylistSummary(10, "ToDelete", false, 0),
            new PlaylistSummary(20, "ToKeep", false, 1)
        ]);
        await vm.InitializeAsync();

        vm.SelectPlaylistCommand.Execute(vm.ManualPlaylists[0]);
        await vm.DeletePlaylistCommand.ExecuteAsync(vm.ManualPlaylists[0]);

        Assert.Single(vm.ManualPlaylists);
        Assert.Equal("ToKeep", vm.ManualPlaylists[0].Name);
        playlistLibrary.Verify(x => x.DeletePlaylistAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        // Should have selected default after deletion
        Assert.Equal(vm.DefaultPlaylist, vm.SelectedItem);
    }

    [Fact]
    public async Task PlaylistCreatedMessage_AddsToManualPlaylists()
    {
        var (vm, messenger, _, _) = CreateViewModel([]);
        await vm.InitializeAsync();

        messenger.Send(new PlaylistCreatedMessage(new PlaylistCreatedMessageData(55, "Fresh")));

        Assert.Single(vm.ManualPlaylists);
        Assert.Equal(55, vm.ManualPlaylists[0].PlaylistId);
        Assert.Equal("Fresh", vm.ManualPlaylists[0].Name);
    }

    [Fact]
    public async Task PlaylistDeletedMessage_RemovesFromManualPlaylists()
    {
        var (vm, messenger, _, _) = CreateViewModel([
            new PlaylistSummary(10, "A", false, 0)
        ]);
        await vm.InitializeAsync();

        messenger.Send(new PlaylistDeletedMessage(new PlaylistDeletedMessageData(10)));

        Assert.Empty(vm.ManualPlaylists);
    }

    [Fact]
    public async Task ActivePlaylistTracking_UpdatesIsActive()
    {
        var (vm, _, _, queueState) = CreateViewModel([
            new PlaylistSummary(10, "A", false, 0)
        ]);
        await vm.InitializeAsync();

        Assert.False(vm.ManualPlaylists[0].IsActive);

        queueState.SetActiveNamedPlaylistId(10);

        Assert.True(vm.ManualPlaylists[0].IsActive);
        Assert.False(vm.DefaultPlaylist.IsActive);
    }

    [Fact]
    public async Task BeginRename_SkipsDefaultPlaylist()
    {
        var (vm, _, _, _) = CreateViewModel([]);
        await vm.InitializeAsync();

        vm.BeginRenameCommand.Execute(vm.DefaultPlaylist);

        Assert.False(vm.DefaultPlaylist.IsRenaming);
    }

    private static (
        PlaylistSidebarViewModel ViewModel,
        WeakReferenceMessenger Messenger,
        Mock<IPlaylistLibraryService> PlaylistLibrary,
        PlaylistQueueState QueueState) CreateViewModel(
        IReadOnlyList<PlaylistSummary> seedPlaylists)
    {
        var logger = new Mock<ILogger>();
        logger.Setup(x => x.ForContext(It.IsAny<Type>())).Returns(logger.Object);

        var messenger = new WeakReferenceMessenger();
        var playlistLibrary = new Mock<IPlaylistLibraryService>();

        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(seedPlaylists);

        var nextId = seedPlaylists.Count == 0 ? 1 : seedPlaylists.Max(x => x.Id) + 1;
        playlistLibrary
            .Setup(x => x.CreatePlaylistAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) =>
                new PlaylistSummary(nextId++, name, false, 0));

        playlistLibrary
            .Setup(x => x.RenamePlaylistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        playlistLibrary
            .Setup(x => x.DeletePlaylistAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        playlistLibrary
            .Setup(x => x.SetPinnedAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var queueState = new PlaylistQueueState(new PlaylistQueue());

        var vm = new PlaylistSidebarViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            playlistLibrary.Object,
            queueState);

        return (vm, messenger, playlistLibrary, queueState);
    }
}
