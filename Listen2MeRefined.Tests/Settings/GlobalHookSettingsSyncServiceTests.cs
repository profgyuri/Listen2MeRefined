using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Utils;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Settings;

public sealed class GlobalHookSettingsSyncServiceTests
{
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
}
