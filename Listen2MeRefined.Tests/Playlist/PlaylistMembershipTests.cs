using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Infrastructure.Playlist;
using MediatR;
using Moq;

namespace Listen2MeRefined.Tests.PlaylistServices;

public class PlaylistMembershipTests
{
    [Fact]
    public async Task GetContextMenuPlaylistsAsync_SingleSong_ReturnsMembershipFromLibraryService()
    {
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetMembershipBySongPathAsync("song-a.mp3", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaylistMembershipInfo(10, "Gym", true)]);

        var sut = new PlaylistMembership(
            playlistLibrary.Object,
            Mock.Of<IMediator>(),
            new WeakReferenceMessenger());

        var result = await sut.GetPlaylistMembershipInfoAsync(["song-a.mp3"], activePlaylistId: null);

        Assert.Single(result);
        Assert.Equal(10, result[0].PlaylistId);
        Assert.True(result[0].ContainsSong);
        playlistLibrary.Verify(
            x => x.GetMembershipBySongPathAsync("song-a.mp3", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetContextMenuPlaylistsAsync_MultipleSongs_MapsCheckedStateFromActivePlaylistId()
    {
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        playlistLibrary
            .Setup(x => x.GetAllPlaylistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaylistSummary(1, "One"), new PlaylistSummary(2, "Two")]);

        var sut = new PlaylistMembership(
            playlistLibrary.Object,
            Mock.Of<IMediator>(),
            new WeakReferenceMessenger());

        var result = await sut.GetPlaylistMembershipInfoAsync(["song-a.mp3", "song-b.mp3"], activePlaylistId: 2);

        Assert.Equal(2, result.Count);
        Assert.False(result.Single(x => x.PlaylistId == 1).ContainsSong);
        Assert.True(result.Single(x => x.PlaylistId == 2).ContainsSong);
    }

    [Fact]
    public async Task TogglePlaylistMembershipAsync_RemoveNotAllowed_DoesNothing()
    {
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        var mediator = new Mock<IMediator>();
        var sut = new PlaylistMembership(
            playlistLibrary.Object,
            mediator.Object,
            new WeakReferenceMessenger());

        await sut.TogglePlaylistMembershipAsync(
            playlistId: 12,
            selectedSongPaths: ["song-a.mp3"],
            shouldContain: false,
            allowRemove: false);

        playlistLibrary.Verify(
            x => x.RemoveSongsByPathAsync(It.IsAny<int>(), It.IsAny<IEnumerable<string?>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mediator.Verify(
            x => x.Publish(It.IsAny<PlaylistMembershipChangedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddToNewPlaylistAsync_ValidInput_CreatesPlaylistAddsSongsAndPublishesNotifications()
    {
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        var mediator = new Mock<IMediator>();
        playlistLibrary
            .Setup(x => x.CreatePlaylistAsync("Fresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaylistSummary(55, "Fresh"));

        var sut = new PlaylistMembership(
            playlistLibrary.Object,
            mediator.Object,
            new WeakReferenceMessenger());

        await sut.AddToNewPlaylistAsync("Fresh", ["a.mp3", "b.mp3"]);

        playlistLibrary.Verify(
            x => x.AddSongsByPathAsync(
                55,
                It.Is<IEnumerable<string?>>(paths => paths.Contains("a.mp3") && paths.Contains("b.mp3")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mediator.Verify(
            x => x.Publish(It.Is<PlaylistCreatedNotification>(n => n.PlaylistId == 55 && n.Name == "Fresh"), It.IsAny<CancellationToken>()),
            Times.Once);
        mediator.Verify(
            x => x.Publish(It.Is<PlaylistMembershipChangedNotification>(n => n.PlaylistId == 55), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
