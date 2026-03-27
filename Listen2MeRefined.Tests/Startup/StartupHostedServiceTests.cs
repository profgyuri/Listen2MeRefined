using Listen2MeRefined.Application.Startup;
using Moq;
using Serilog;
using Listen2MeRefined.WPF.Startup;

namespace Listen2MeRefined.Tests.Startup;

public sealed class StartupHostedServiceTests
{
    [Fact]
    public async Task StartAsync_State_InvokesStartupManager()
    {
        var startupManager = new Mock<IStartupManager>();
        startupManager
            .Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hostedService = new StartupHostedService(startupManager.Object, Mock.Of<ILogger>());

        await hostedService.StartAsync(CancellationToken.None);

        startupManager.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_State_ForwardsCancellationToken()
    {
        var startupManager = new Mock<IStartupManager>();
        CancellationToken capturedToken = default;
        startupManager
            .Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedToken = token)
            .Returns(Task.CompletedTask);

        var hostedService = new StartupHostedService(startupManager.Object, Mock.Of<ILogger>());
        using var cancellationSource = new CancellationTokenSource();

        await hostedService.StartAsync(cancellationSource.Token);

        Assert.Equal(cancellationSource.Token, capturedToken);
    }
}
