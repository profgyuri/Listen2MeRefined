using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Settings.Playback;
using Moq;

namespace Listen2MeRefined.Tests.Settings.Playback;

public sealed class PlaybackDefaultsServiceTests
{
    [Fact]
    public void LoadStartupDefaults_ClampsVolume()
    {
        var settings = new AppSettings { StartupVolume = 5f, StartMuted = true };
        var manager = CreateSettingsManager(settings);
        var sut = new PlaybackDefaultsService(manager.Object);

        var result = sut.LoadStartupDefaults();

        Assert.Equal(1f, result.StartupVolume);
        Assert.True(result.StartMuted);
    }

    [Fact]
    public void PersistPlaybackDefaults_UpdatesMuteAndVolumeWhenVolumeAboveZero()
    {
        var settings = new AppSettings { StartupVolume = 0.2f, StartMuted = true };
        var manager = CreateSettingsManager(settings);
        var sut = new PlaybackDefaultsService(manager.Object);

        sut.PersistPlaybackDefaults(0.63f, false);

        Assert.False(settings.StartMuted);
        Assert.InRange(settings.StartupVolume, 0.629f, 0.631f);
    }

    [Fact]
    public void VolumePercentConversions_MapCorrectly()
    {
        var settings = new AppSettings();
        var manager = CreateSettingsManager(settings);
        var sut = new PlaybackDefaultsService(manager.Object);

        Assert.Equal(42, sut.ToVolumePercent(0.42f));
        Assert.InRange(sut.FromVolumePercent(42), 0.419f, 0.421f);
    }

    private static Mock<ISettingsManager<AppSettings>> CreateSettingsManager(AppSettings settings)
    {
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);
        settingsManager
            .Setup(x => x.SaveSettings(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(apply => apply(settings));
        return settingsManager;
    }
}
