using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Scanning.Folders;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;

namespace Listen2MeRefined.Tests.Settings;

public sealed class AppSettingsWriterTests
{
    [Fact]
    public void SetMusicFolders_NormalizesTrimsAndDeduplicates()
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
    public void SetPinnedFolders_NormalizesTrimsAndDeduplicates()
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
    public void SetMutedDroppedSongFolders_PersistsValue()
    {
        var settings = new AppSettings();
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriter(manager.Object);

        sut.SetMutedDroppedSongFolders([@"C:\Muted"]);

        Assert.Single(settings.MutedDroppedSongFolders);
        Assert.Equal(@"C:\Muted", settings.MutedDroppedSongFolders[0]);
    }


    [Fact]
    public void SetMutedDroppedSongFolders_PersistsValue()
    {
        var settings = new AppSettings();
        var manager = new FakeSettingsManager(settings);
        var sut = new AppSettingsWriter(manager);

        sut.SetMutedDroppedSongFolders([@"C:\Muted"]);

        Assert.Single(settings.MutedDroppedSongFolders);
        Assert.Equal(@"C:\Muted", settings.MutedDroppedSongFolders[0]);
    }

    [Fact]
    public void SetMusicFolders_WithRequests_PersistsRecursionFlags()
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
    public void SetScanOnStartup_PersistsValue()
    {
        var settings = new AppSettings { ScanOnStartup = true };
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriter(manager.Object);

        sut.SetScanOnStartup(false);

        Assert.False(settings.ScanOnStartup);
    }

    [Fact]
    public void SetFolderIncludeSubdirectories_UpdatesExistingFolder()
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
    public void SetTaskStatusProgressSettings_PersistsValues()
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
