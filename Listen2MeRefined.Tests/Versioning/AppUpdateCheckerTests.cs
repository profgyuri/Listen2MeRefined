using Listen2MeRefined.Infrastructure.Versioning;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Versioning;

public sealed class AppUpdateCheckerTests
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
}
