using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Infrastructure.Playlist;
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
        var messenger = new WeakReferenceMessenger();
        var probe = new MessageProbe();
        messenger.Register<MessageProbe, PlaylistMembershipChangedMessage>(
            probe,
            static (recipient, message) => recipient.MembershipChangedPlaylistIds.Add(message.Value));

        var sut = new PlaylistMembership(
            playlistLibrary.Object,
            messenger);

        await sut.TogglePlaylistMembershipAsync(
            playlistId: 12,
            selectedSongPaths: ["song-a.mp3"],
            shouldContain: false,
            allowRemove: false);

        playlistLibrary.Verify(
            x => x.RemoveSongsByPathAsync(It.IsAny<int>(), It.IsAny<IEnumerable<string?>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Empty(probe.MembershipChangedPlaylistIds);
    }

    [Fact]
    public async Task AddToNewPlaylistAsync_ValidInput_CreatesPlaylistAddsSongsAndPublishesNotifications()
    {
        var playlistLibrary = new Mock<IPlaylistLibraryService>();
        var messenger = new WeakReferenceMessenger();
        var probe = new MessageProbe();
        messenger.Register<MessageProbe, PlaylistCreatedMessage>(
            probe,
            static (recipient, message) => recipient.Created = message.Value);
        messenger.Register<MessageProbe, PlaylistMembershipChangedMessage>(
            probe,
            static (recipient, message) => recipient.MembershipChangedPlaylistIds.Add(message.Value));

        playlistLibrary
            .Setup(x => x.CreatePlaylistAsync("Fresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaylistSummary(55, "Fresh"));

        var sut = new PlaylistMembership(
            playlistLibrary.Object,
            messenger);

        await sut.AddToNewPlaylistAsync("Fresh", ["a.mp3", "b.mp3"]);

        playlistLibrary.Verify(
            x => x.AddSongsByPathAsync(
                55,
                It.Is<IEnumerable<string?>>(paths => paths.Contains("a.mp3") && paths.Contains("b.mp3")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(probe.Created);
        Assert.Equal(55, probe.Created!.PlaylistId);
        Assert.Equal("Fresh", probe.Created!.Name);
        Assert.Equal([55], probe.MembershipChangedPlaylistIds);
    }

    private sealed class MessageProbe
    {
        public PlaylistCreatedMessageData? Created { get; set; }

        public List<int> MembershipChangedPlaylistIds { get; } = [];
    }
}
