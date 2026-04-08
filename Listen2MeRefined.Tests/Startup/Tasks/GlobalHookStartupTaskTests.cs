using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Infrastructure.Startup.Tasks;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Startup.Tasks;

public class GlobalHookStartupTaskTests
{
    [Fact]
    public async Task RunAsync_AlwaysRegistersHooks()
    {
        var globalHook = new Mock<IGlobalHook>();
        globalHook.Setup(x => x.RegisterAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var startupTask = new GlobalHookStartupTask(
            globalHook.Object,
            Mock.Of<ILogger>());

        await startupTask.RunAsync(CancellationToken.None);

        globalHook.Verify(x => x.RegisterAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
