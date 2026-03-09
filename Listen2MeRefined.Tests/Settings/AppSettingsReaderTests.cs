using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;

namespace Listen2MeRefined.Tests.Settings;

public sealed class AppSettingsReaderTests
{
    [Fact]
    public void ReadsTypedValuesFromSettings()
    {
        var settings = new AppSettings
        {
            FontFamily = "Segoe UI",
            NewSongWindowPosition = "Always on top",
            AudioOutputDeviceName = "Device A",
            MusicFolders = [new MusicFolderModel(@"C:\Music", true)],
            ScanOnStartup = true,
            EnableGlobalMediaKeys = true,
            EnableCornerNowPlayingPopup = false,
            CornerTriggerSizePx = 16,
            CornerTriggerDebounceMs = 20,
            StartupVolume = 0.35f,
            StartMuted = false,
            AutoCheckUpdatesOnStartup = true,
            AutoScanOnFolderAdd = false,
            MutedDroppedSongFolders = [@"C:\Muted"],
            ShowTaskPercentage = true,
            TaskPercentageReportInterval = 5,
            ShowScanMilestoneCount = true,
            ScanMilestoneInterval = 50,
            ScanMilestoneBasis = TaskStatusCountBasis.Remaining,
            FolderBrowserStartAtLastLocation = true,
            LastBrowsedFolder = @"C:\Music",
            PinnedFolders = [@"C:\Pinned"]
        };

        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsReader(manager.Object);

        Assert.Equal("Segoe UI", sut.GetFontFamily());
        Assert.Equal("Always on top", sut.GetNewSongWindowPosition());
        Assert.Equal("Device A", sut.GetAudioOutputDeviceName());
        Assert.Single(sut.GetMusicFolders());
        Assert.Equal(@"C:\Music", Assert.Single(sut.GetMusicFolderRequests()).Path);
        Assert.True(Assert.Single(sut.GetMusicFolderRequests()).IncludeSubdirectories);
        Assert.True(sut.GetScanOnStartup());
        Assert.True(sut.GetEnableGlobalMediaKeys());
        Assert.False(sut.GetEnableCornerNowPlayingPopup());
        Assert.Equal((short)16, sut.GetCornerTriggerSizePx());
        Assert.Equal((short)20, sut.GetCornerTriggerDebounceMs());
        Assert.Equal(0.35f, sut.GetStartupVolume());
        Assert.False(sut.GetStartMuted());
        Assert.True(sut.GetAutoCheckUpdatesOnStartup());
        Assert.False(sut.GetAutoScanOnFolderAdd());
        Assert.Single(sut.GetMutedDroppedSongFolders());
        Assert.Equal(@"C:\Muted", sut.GetMutedDroppedSongFolders()[0]);
        Assert.True(sut.GetShowTaskPercentage());
        Assert.Equal((short)5, sut.GetTaskPercentageReportInterval());
        Assert.True(sut.GetShowScanMilestoneCount());
        Assert.Equal((short)50, sut.GetScanMilestoneInterval());
        Assert.Equal(TaskStatusCountBasis.Remaining, sut.GetScanMilestoneBasis());
        Assert.True(sut.GetFolderBrowserStartAtLastLocation());
        Assert.Equal(@"C:\Music", sut.GetLastBrowsedFolder());
        Assert.Equal(@"C:\Pinned", Assert.Single(sut.GetPinnedFolders()));
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
