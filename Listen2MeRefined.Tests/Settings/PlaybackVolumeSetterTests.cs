using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;

namespace Listen2MeRefined.Tests.Settings;

public sealed class PlaybackVolumeSetterTests
{
    [Fact]
    public void ApplyStartupDefaults_StartMuted_SetsZeroVolumeAndMutedState()
    {
        var player = CreatePlayer(initialVolume: 1f);
        var defaults = new Mock<IPlaybackDefaultsService>();
        defaults.Setup(x => x.LoadStartupDefaults()).Returns(new PlaybackDefaultsSnapshot(0.75f, StartMuted: true));
        var sut = new PlaybackVolumeSetter(player.Object, defaults.Object);

        var state = sut.ApplyStartupDefaults();

        Assert.InRange(player.Object.Volume, -0.001f, 0.001f);
        Assert.True(state.IsMuted);
        Assert.InRange(state.Volume, -0.001f, 0.001f);
    }

    [Fact]
    public void SetVolume_FromMutedToAudible_UnmutesAndPersistsDefaults()
    {
        var player = CreatePlayer(initialVolume: 1f);
        var defaults = new Mock<IPlaybackDefaultsService>();
        defaults.Setup(x => x.LoadStartupDefaults()).Returns(new PlaybackDefaultsSnapshot(0.2f, StartMuted: true));
        var sut = new PlaybackVolumeSetter(player.Object, defaults.Object);
        sut.ApplyStartupDefaults();

        var change = sut.SetVolume(0.63f);

        Assert.True(change.HasVolumeChanged);
        Assert.False(change.IsMuted);
        Assert.InRange(player.Object.Volume, 0.629f, 0.631f);
        defaults.Verify(x => x.PersistPlaybackDefaults(
            It.Is<float>(value => value >= 0.629f && value <= 0.631f),
            false), Times.Once);
    }

    [Fact]
    public void SetVolume_NoEffectiveChange_DoesNotPersistDefaults()
    {
        var player = CreatePlayer(initialVolume: 1f);
        var defaults = new Mock<IPlaybackDefaultsService>();
        defaults.Setup(x => x.LoadStartupDefaults()).Returns(new PlaybackDefaultsSnapshot(0.4f, StartMuted: false));
        var sut = new PlaybackVolumeSetter(player.Object, defaults.Object);
        sut.ApplyStartupDefaults();

        var change = sut.SetVolume(0.4f);

        Assert.False(change.HasVolumeChanged);
        defaults.Verify(x => x.PersistPlaybackDefaults(It.IsAny<float>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void ToggleMute_WhenMuted_RestoresLastNonZeroVolume()
    {
        var player = CreatePlayer(initialVolume: 1f);
        var defaults = new Mock<IPlaybackDefaultsService>();
        defaults.Setup(x => x.LoadStartupDefaults()).Returns(new PlaybackDefaultsSnapshot(0.55f, StartMuted: false));
        var sut = new PlaybackVolumeSetter(player.Object, defaults.Object);
        sut.ApplyStartupDefaults();

        var muted = sut.ToggleMute();
        var unmuted = sut.ToggleMute();

        Assert.True(muted.IsMuted);
        Assert.False(unmuted.IsMuted);
        Assert.InRange(player.Object.Volume, 0.549f, 0.551f);
        defaults.Verify(x => x.PersistPlaybackDefaults(0f, true), Times.Once);
        defaults.Verify(x => x.PersistPlaybackDefaults(
            It.Is<float>(value => value >= 0.549f && value <= 0.551f),
            false), Times.Once);
    }

    [Fact]
    public void GetVolumeIconKind_TracksVolumeStateTransitions()
    {
        var player = CreatePlayer(initialVolume: 1f);
        var defaults = new Mock<IPlaybackDefaultsService>();
        defaults.Setup(x => x.LoadStartupDefaults()).Returns(new PlaybackDefaultsSnapshot(0.2f, StartMuted: false));
        var sut = new PlaybackVolumeSetter(player.Object, defaults.Object);
        sut.ApplyStartupDefaults();

        Assert.Equal("VolumeLow", sut.GetVolumeIconKind());

        sut.SetVolume(0.5f);
        Assert.Equal("VolumeMedium", sut.GetVolumeIconKind());

        sut.SetVolume(0.9f);
        Assert.Equal("VolumeHigh", sut.GetVolumeIconKind());

        sut.ToggleMute();
        Assert.Equal("VolumeOff", sut.GetVolumeIconKind());
    }

    private static Mock<IMusicPlayerController> CreatePlayer(float initialVolume)
    {
        var player = new Mock<IMusicPlayerController>();
        player.SetupProperty(x => x.Volume, initialVolume);
        return player;
    }
}
