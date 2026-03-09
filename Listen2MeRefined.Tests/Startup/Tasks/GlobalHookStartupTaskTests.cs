using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.Infrastructure.Startup.Tasks;
using Listen2MeRefined.Infrastructure.Utils;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Startup.Tasks;

public class GlobalHookStartupTaskTests
{
    [Fact]
    public async Task RunAsync_DoesNotRegisterHooks_WhenBothFeaturesAreDisabled()
    {
        var globalHook = new Mock<IGlobalHook>();
        var settings = new Mock<ISettingsManager<AppSettings>>();
        settings.SetupGet(x => x.Settings).Returns(new AppSettings
        {
            EnableGlobalMediaKeys = false,
            EnableCornerNowPlayingPopup = false
        });

        var startupTask = new GlobalHookStartupTask(
            globalHook.Object,
            settings.Object,
            Mock.Of<ILogger>());

        await startupTask.RunAsync(CancellationToken.None);

        globalHook.Verify(x => x.RegisterAsync(), Times.Never);
    }

    [Fact]
    public async Task RunAsync_RegistersHooks_WhenAtLeastOneFeatureIsEnabled()
    {
        var globalHook = new Mock<IGlobalHook>();
        globalHook.Setup(x => x.RegisterAsync()).Returns(Task.CompletedTask);

        var settings = new Mock<ISettingsManager<AppSettings>>();
        settings.SetupGet(x => x.Settings).Returns(new AppSettings
        {
            EnableGlobalMediaKeys = true,
            EnableCornerNowPlayingPopup = false
        });

        var startupTask = new GlobalHookStartupTask(
            globalHook.Object,
            settings.Object,
            Mock.Of<ILogger>());

        await startupTask.RunAsync(CancellationToken.None);

        globalHook.Verify(x => x.RegisterAsync(), Times.Once);
    }
}

