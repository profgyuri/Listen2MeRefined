using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Scanning.Folders;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;

namespace Listen2MeRefined.Tests.Services;

public sealed class AppSettingsServicesTests
{
    [Fact]
    public void AppSettingsReadService_ReadsTypedValuesFromSettings()
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
        Assert.True(sut.GetShowTaskPercentage());
        Assert.Equal((short)5, sut.GetTaskPercentageReportInterval());
        Assert.True(sut.GetShowScanMilestoneCount());
        Assert.Equal((short)50, sut.GetScanMilestoneInterval());
        Assert.Equal(TaskStatusCountBasis.Remaining, sut.GetScanMilestoneBasis());
        Assert.True(sut.GetFolderBrowserStartAtLastLocation());
        Assert.Equal(@"C:\Music", sut.GetLastBrowsedFolder());
        Assert.Equal(@"C:\Pinned", Assert.Single(sut.GetPinnedFolders()));
    }

    [Fact]
    public void AppSettingsWriteService_SetMusicFolders_NormalizesTrimsAndDeduplicates()
    {
        var settings = new AppSettings();
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriter(manager.Object);

        sut.SetMusicFolders([@" C:\Music ", @"c:\music", @"D:\Rock"]);

        Assert.Equal(2, settings.MusicFolders.Count);
        Assert.Contains(settings.MusicFolders, x => x.FullPath == @"C:\Music");
        Assert.Contains(settings.MusicFolders, x => x.FullPath == @"D:\Rock");
    }

    [Fact]
    public void AppSettingsWriteService_SetPinnedFolders_NormalizesTrimsAndDeduplicates()
    {
        var settings = new AppSettings();
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriter(manager.Object);

        sut.SetPinnedFolders([@" C:\A ", @"c:\a", @"D:\B"]);

        Assert.Equal(2, settings.PinnedFolders.Count);
        Assert.Contains(@"C:\A", settings.PinnedFolders, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(@"D:\B", settings.PinnedFolders, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppSettingsWriteService_SetMusicFolders_WithRequests_PersistsRecursionFlags()
    {
        var settings = new AppSettings();
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriter(manager.Object);

        sut.SetMusicFolders(
        [
            new FolderScanRequest(@"C:\Music", true),
            new FolderScanRequest(@"D:\Rock", false)
        ]);

        Assert.Equal(2, settings.MusicFolders.Count);
        Assert.Contains(settings.MusicFolders, x => x.FullPath == @"C:\Music" && x.IncludeSubdirectories);
        Assert.Contains(settings.MusicFolders, x => x.FullPath == @"D:\Rock" && !x.IncludeSubdirectories);
    }

    [Fact]
    public void AppSettingsWriteService_SetScanOnStartup_PersistsValue()
    {
        var settings = new AppSettings { ScanOnStartup = true };
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriter(manager.Object);

        sut.SetScanOnStartup(false);

        Assert.False(settings.ScanOnStartup);
    }

    [Fact]
    public void AppSettingsWriteService_SetFolderIncludeSubdirectories_UpdatesExistingFolder()
    {
        var settings = new AppSettings
        {
            MusicFolders = [new MusicFolderModel(@"C:\Music", false)]
        };
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriter(manager.Object);

        sut.SetFolderIncludeSubdirectories(@"C:\Music", true);

        Assert.True(settings.MusicFolders.Single().IncludeSubdirectories);
    }

    [Fact]
    public void AppSettingsWriteService_SetTaskStatusProgressSettings_PersistsValues()
    {
        var settings = new AppSettings();
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriter(manager.Object);

        sut.SetShowTaskPercentage(false);
        sut.SetTaskPercentageReportInterval(7);
        sut.SetShowScanMilestoneCount(true);
        sut.SetScanMilestoneInterval(40);
        sut.SetScanMilestoneBasis(TaskStatusCountBasis.Remaining);

        Assert.False(settings.ShowTaskPercentage);
        Assert.Equal((short)7, settings.TaskPercentageReportInterval);
        Assert.True(settings.ShowScanMilestoneCount);
        Assert.Equal((short)40, settings.ScanMilestoneInterval);
        Assert.Equal(TaskStatusCountBasis.Remaining, settings.ScanMilestoneBasis);
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
