using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.WPF.ErrorHandling;
using Moq;
using Serilog;
using System.Reflection;

namespace Listen2MeRefined.Tests.ErrorHandling;

public sealed class CrashAwareErrorHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCalled_DoesNotShowCrashDialog()
    {
        ResetFatalGuard();

        var crashDialog = new Mock<ICrashDialogService>(MockBehavior.Strict);
        var logLocation = new Mock<ILogLocationService>(MockBehavior.Strict);

        var sut = new CrashAwareErrorHandler(
            CreateLogger(),
            crashDialog.Object,
            logLocation.Object);

        await sut.HandleAsync(new InvalidOperationException("recoverable"), "TestContext");

        crashDialog.Verify(
            x => x.ShowAsync(
                It.IsAny<Exception>(),
                It.IsAny<UnhandledErrorContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleUnhandledAsync_WhenDialogRequestsOpenLogs_OpensLogDirectory()
    {
        ResetFatalGuard();

        var crashDialog = new Mock<ICrashDialogService>();
        crashDialog
            .Setup(x => x.ShowAsync(
                It.IsAny<Exception>(),
                It.IsAny<UnhandledErrorContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CrashDialogAction.OpenLogsAndExit);

        var logLocation = new Mock<ILogLocationService>();
        logLocation
            .SetupGet(x => x.LogDirectoryPath)
            .Returns(@"C:\Logs\Listen2MeRefined");

        var sut = new CrashAwareErrorHandler(
            CreateLogger(),
            crashDialog.Object,
            logLocation.Object);

        await sut.HandleUnhandledAsync(
            new InvalidOperationException("fatal"),
            new UnhandledErrorContext(
                UnhandledErrorSource.Dispatcher,
                IsTerminating: true,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Context: "DispatcherUnhandledException"));

        logLocation.Verify(x => x.OpenLogDirectory(), Times.Once);
    }

    [Fact]
    public async Task HandleUnhandledAsync_WhenCalledTwice_ShowsCrashDialogOnlyOnce()
    {
        ResetFatalGuard();

        var crashDialog = new Mock<ICrashDialogService>();
        crashDialog
            .Setup(x => x.ShowAsync(
                It.IsAny<Exception>(),
                It.IsAny<UnhandledErrorContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CrashDialogAction.Exit);

        var logLocation = new Mock<ILogLocationService>();

        var sut = new CrashAwareErrorHandler(
            CreateLogger(),
            crashDialog.Object,
            logLocation.Object);

        var context = new UnhandledErrorContext(
            UnhandledErrorSource.TaskScheduler,
            IsTerminating: true,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Context: "UnobservedTaskException");

        await sut.HandleUnhandledAsync(new InvalidOperationException("fatal1"), context);
        await sut.HandleUnhandledAsync(new InvalidOperationException("fatal2"), context);

        crashDialog.Verify(
            x => x.ShowAsync(
                It.IsAny<Exception>(),
                It.IsAny<UnhandledErrorContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ILogger CreateLogger()
    {
        return new Serilog.LoggerConfiguration()
            .MinimumLevel.Verbose()
            .CreateLogger();
    }

    private static void ResetFatalGuard()
    {
        var field = typeof(CrashAwareErrorHandler).GetField(
            "_isHandlingFatal",
            BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, 0);
    }
}
