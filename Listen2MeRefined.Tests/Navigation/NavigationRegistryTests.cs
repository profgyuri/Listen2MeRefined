using Listen2MeRefined.Infrastructure.Navigation;

namespace Listen2MeRefined.Tests.Navigation;

public sealed class NavigationRegistryTests
{
    [Fact]
    public void TryResolveOfTViewModel_State_ReturnsRegisteredTarget()
    {
        var sut = new NavigationRegistry();
        sut.Register<TestViewModel>("test/home");

        var resolved = sut.TryResolve<TestViewModel>(out var target);

        Assert.True(resolved);
        Assert.NotNull(target);
        Assert.Equal("test/home", target!.Route);
        Assert.Equal(typeof(TestViewModel), target.ViewModelType);
    }

    [Fact]
    public void TryResolveOfTViewModel_State_ReturnsFalseWhenMissing()
    {
        var sut = new NavigationRegistry();

        var resolved = sut.TryResolve<TestViewModel>(out var target);

        Assert.False(resolved);
        Assert.Null(target);
    }

    [Fact]
    public void Register_State_ThrowsWhenViewModelAlreadyRegisteredToDifferentRoute()
    {
        var sut = new NavigationRegistry();
        sut.Register<TestViewModel>("test/home");

        var exception = Assert.Throws<InvalidOperationException>(() => sut.Register<TestViewModel>("test/secondary"));

        Assert.Contains("already registered", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(sut.TryResolve("test/home", out var existingTarget));
        Assert.NotNull(existingTarget);
        Assert.False(sut.TryResolve("test/secondary", out _));
    }

    private sealed class TestViewModel
    {
    }
}
