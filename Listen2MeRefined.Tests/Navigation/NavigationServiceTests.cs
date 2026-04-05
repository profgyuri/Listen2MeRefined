using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels;
using Listen2MeRefined.Infrastructure.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Navigation;

public sealed class NavigationServiceTests
{
    [Fact]
    public async Task NavigateAsyncOfTViewModel_State_UpdatesNavigationState()
    {
        var viewModel = new PlainViewModel();
        var services = new ServiceCollection();
        services.AddSingleton(viewModel);
        var serviceProvider = services.BuildServiceProvider();

        var registry = new NavigationRegistry();
        registry.Register<PlainViewModel>("plain/home");

        var state = new NavigationState();
        var sut = new NavigationService(
            serviceProvider,
            registry,
            state,
            Mock.Of<IInitializationTracker>(),
            Mock.Of<IErrorHandler>(),
            CreateLogger().Object);

        await sut.NavigateAsync<PlainViewModel>();

        Assert.Equal("plain/home", state.CurrentRoute);
        Assert.Same(viewModel, state.CurrentViewModel);
    }

    [Fact]
    public async Task NavigateAsyncOfTViewModel_State_InitializesInitializableViewModel()
    {
        var viewModel = new InitializableViewModel();
        var services = new ServiceCollection();
        services.AddSingleton(viewModel);
        var serviceProvider = services.BuildServiceProvider();

        var registry = new NavigationRegistry();
        registry.Register<InitializableViewModel>("initializable/home");

        var state = new NavigationState();
        var initializationTracker = new Mock<IInitializationTracker>();
        initializationTracker
            .Setup(x => x.EnsureInitializedAsync(viewModel, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new NavigationService(
            serviceProvider,
            registry,
            state,
            initializationTracker.Object,
            Mock.Of<IErrorHandler>(),
            CreateLogger().Object);

        using var cts = new CancellationTokenSource();
        await sut.NavigateAsync<InitializableViewModel>(cts.Token);

        initializationTracker.Verify(x => x.EnsureInitializedAsync(viewModel, cts.Token), Times.Once);
        Assert.Equal("initializable/home", state.CurrentRoute);
        Assert.Same(viewModel, state.CurrentViewModel);
    }

    [Fact]
    public async Task NavigateAsyncOfTViewModel_State_ThrowsWhenViewModelIsNotRegistered()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var state = new NavigationState();
        var sut = new NavigationService(
            serviceProvider,
            new NavigationRegistry(),
            state,
            Mock.Of<IInitializationTracker>(),
            Mock.Of<IErrorHandler>(),
            CreateLogger().Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.NavigateAsync<MissingViewModel>());

        Assert.Contains(typeof(MissingViewModel).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Equal(string.Empty, state.CurrentRoute);
        Assert.Null(state.CurrentViewModel);
    }

    [Fact]
    public async Task NavigateAsync_PreviousViewModelIsDisposable_DisposesPrevious()
    {
        var first = new DisposableViewModel();
        var second = new PlainViewModel();

        var services = new ServiceCollection();
        services.AddSingleton(first);
        services.AddSingleton(second);
        var serviceProvider = services.BuildServiceProvider();

        var registry = new NavigationRegistry();
        registry.Register<DisposableViewModel>("tab/first");
        registry.Register<PlainViewModel>("tab/second");

        var state = new NavigationState();
        var sut = new NavigationService(
            serviceProvider,
            registry,
            state,
            Mock.Of<IInitializationTracker>(),
            Mock.Of<IErrorHandler>(),
            CreateLogger().Object);

        await sut.NavigateAsync<DisposableViewModel>();
        Assert.False(first.IsDisposed);

        await sut.NavigateAsync<PlainViewModel>();
        Assert.True(first.IsDisposed);
    }

    [Fact]
    public async Task NavigateAsync_SameViewModel_DoesNotDispose()
    {
        var viewModel = new DisposableViewModel();
        var services = new ServiceCollection();
        services.AddSingleton(viewModel);
        var serviceProvider = services.BuildServiceProvider();

        var registry = new NavigationRegistry();
        registry.Register<DisposableViewModel>("tab/home");

        var state = new NavigationState();
        var sut = new NavigationService(
            serviceProvider,
            registry,
            state,
            Mock.Of<IInitializationTracker>(),
            Mock.Of<IErrorHandler>(),
            CreateLogger().Object);

        await sut.NavigateAsync<DisposableViewModel>();
        await sut.NavigateAsync<DisposableViewModel>();

        Assert.False(viewModel.IsDisposed);
    }

    private static Mock<ILogger> CreateLogger()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        return logger;
    }

    private sealed class PlainViewModel
    {
    }

    private sealed class InitializableViewModel : IInitializeAsync
    {
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class MissingViewModel
    {
    }

    private sealed class DisposableViewModel : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }
}
