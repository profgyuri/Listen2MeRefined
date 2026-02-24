using Listen2MeRefined.Infrastructure.Settings.WindowPosition;

namespace Listen2MeRefined.Tests.Settings.WindowPosition;

public sealed class WindowPositionPolicyServiceTests
{
    [Fact]
    public void IsTopmost_MatchesAlwaysOnTopSetting()
    {
        var sut = new WindowPositionPolicyService();

        Assert.True(sut.IsTopmost("Always on top"));
        Assert.False(sut.IsTopmost("Default"));
        Assert.False(sut.IsTopmost(null));
    }
}
