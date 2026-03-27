using Listen2MeRefined.Infrastructure.Startup;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Startup.ShellOpen;

public sealed class ExternalAudioOpenInboxTests
{
    [Fact]
    public void RegisterConsumer_ReplaysPendingPaths()
    {
        var sut = new ExternalAudioOpenInbox(CreateLogger().Object);
        var received = new List<IReadOnlyList<string>>();

        sut.Enqueue(["  a.mp3", "A.mp3", "b.mp3"]);
        using var _ = sut.RegisterConsumer(paths => received.Add(paths), replayPending: true);

        var replay = Assert.Single(received);
        Assert.Equal(["a.mp3", "b.mp3"], replay);
    }

    [Fact]
    public void RegisterConsumer_LiveEnqueue_DeliversImmediately()
    {
        var sut = new ExternalAudioOpenInbox(CreateLogger().Object);
        var received = new List<IReadOnlyList<string>>();

        using var _ = sut.RegisterConsumer(paths => received.Add(paths), replayPending: true);
        sut.Enqueue(["live-a.mp3", "live-b.mp3"]);

        var live = Assert.Single(received);
        Assert.Equal(["live-a.mp3", "live-b.mp3"], live);
    }

    [Fact]
    public void RegisterConsumer_DisposeStopsDelivery()
    {
        var sut = new ExternalAudioOpenInbox(CreateLogger().Object);
        var received = new List<IReadOnlyList<string>>();

        var registration = sut.RegisterConsumer(paths => received.Add(paths), replayPending: true);
        registration.Dispose();
        sut.Enqueue(["ignored.mp3"]);

        Assert.Empty(received);
    }

    private static Mock<ILogger> CreateLogger()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        return logger;
    }
}
