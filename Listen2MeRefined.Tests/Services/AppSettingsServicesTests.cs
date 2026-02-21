using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Storage;
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
            MusicFolders = [new MusicFolderModel(@"C:\Music")],
            ScanOnStartup = true,
            EnableGlobalMediaKeys = true,
            EnableCornerNowPlayingPopup = false,
            CornerTriggerSizePx = 16,
            CornerTriggerDebounceMs = 20,
            StartupVolume = 0.35f,
            StartMuted = false,
            AutoCheckUpdatesOnStartup = true,
            AutoScanOnFolderAdd = false,
            FolderBrowserStartAtLastLocation = true,
            LastBrowsedFolder = @"C:\Music",
            PinnedFolders = [@"C:\Pinned"]
        };

        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsReadService(manager.Object);

        Assert.Equal("Segoe UI", sut.GetFontFamily());
        Assert.Equal("Always on top", sut.GetNewSongWindowPosition());
        Assert.Equal("Device A", sut.GetAudioOutputDeviceName());
        Assert.Single(sut.GetMusicFolders());
        Assert.True(sut.GetScanOnStartup());
        Assert.True(sut.GetEnableGlobalMediaKeys());
        Assert.False(sut.GetEnableCornerNowPlayingPopup());
        Assert.Equal((short)16, sut.GetCornerTriggerSizePx());
        Assert.Equal((short)20, sut.GetCornerTriggerDebounceMs());
        Assert.Equal(0.35f, sut.GetStartupVolume());
        Assert.False(sut.GetStartMuted());
        Assert.True(sut.GetAutoCheckUpdatesOnStartup());
        Assert.False(sut.GetAutoScanOnFolderAdd());
        Assert.True(sut.GetFolderBrowserStartAtLastLocation());
        Assert.Equal(@"C:\Music", sut.GetLastBrowsedFolder());
        Assert.Equal(@"C:\Pinned", Assert.Single(sut.GetPinnedFolders()));
    }

    [Fact]
    public void AppSettingsWriteService_SetMusicFolders_NormalizesTrimsAndDeduplicates()
    {
        var settings = new AppSettings();
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriteService(manager.Object);

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
        var sut = new AppSettingsWriteService(manager.Object);

        sut.SetPinnedFolders([@" C:\A ", @"c:\a", @"D:\B"]);

        Assert.Equal(2, settings.PinnedFolders.Count);
        Assert.Contains(@"C:\A", settings.PinnedFolders, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(@"D:\B", settings.PinnedFolders, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppSettingsWriteService_SetScanOnStartup_PersistsValue()
    {
        var settings = new AppSettings { ScanOnStartup = true };
        var manager = CreateSettingsManager(settings);
        var sut = new AppSettingsWriteService(manager.Object);

        sut.SetScanOnStartup(false);

        Assert.False(settings.ScanOnStartup);
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
