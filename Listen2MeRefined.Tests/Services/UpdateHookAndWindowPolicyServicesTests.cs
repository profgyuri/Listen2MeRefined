using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Settings.WindowPosition;
using Listen2MeRefined.Infrastructure.Utils;
using Listen2MeRefined.Infrastructure.Versioning;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Services;

public sealed class UpdateHookAndWindowPolicyServicesTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_WhenLatest_ReturnsUpToDateMessage()
    {
        var versionChecker = new Mock<IVersionChecker>();
        versionChecker.Setup(x => x.IsLatestAsync()).ReturnsAsync(true);
        var sut = new AppUpdateChecker(versionChecker.Object, Mock.Of<ILogger>());

        var result = await sut.CheckForUpdatesAsync();

        Assert.False(result.IsUpdateAvailable);
        Assert.False(result.CanOpenUpdateLink);
        Assert.Contains("latest version", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNotLatest_ReturnsAvailableMessage()
    {
        var versionChecker = new Mock<IVersionChecker>();
        versionChecker.Setup(x => x.IsLatestAsync()).ReturnsAsync(false);
        var sut = new AppUpdateChecker(versionChecker.Object, Mock.Of<ILogger>());

        var result = await sut.CheckForUpdatesAsync();

        Assert.True(result.IsUpdateAvailable);
        Assert.True(result.CanOpenUpdateLink);
        Assert.Contains("newer version", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncAsync_WhenHooksDisabled_Unregisters()
    {
        var hook = new Mock<IGlobalHook>();
        var sut = new GlobalHookSettingsSyncService(hook.Object, Mock.Of<ILogger>());

        await sut.SyncAsync(enableGlobalMediaKeys: false, enableCornerNowPlayingPopup: false);

        hook.Verify(x => x.Unregister(), Times.Once);
        hook.Verify(x => x.RegisterAsync(), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhenAnyHookEnabled_Registers()
    {
        var hook = new Mock<IGlobalHook>();
        hook.Setup(x => x.RegisterAsync()).Returns(Task.CompletedTask);
        var sut = new GlobalHookSettingsSyncService(hook.Object, Mock.Of<ILogger>());

        await sut.SyncAsync(enableGlobalMediaKeys: true, enableCornerNowPlayingPopup: false);

        hook.Verify(x => x.RegisterAsync(), Times.Once);
        hook.Verify(x => x.Unregister(), Times.Never);
    }

    [Fact]
    public void IsTopmost_MatchesAlwaysOnTopSetting()
    {
        var sut = new WindowPositionPolicyService();

        Assert.True(sut.IsTopmost("Always on top"));
        Assert.False(sut.IsTopmost("Default"));
        Assert.False(sut.IsTopmost(null));
    }
}
