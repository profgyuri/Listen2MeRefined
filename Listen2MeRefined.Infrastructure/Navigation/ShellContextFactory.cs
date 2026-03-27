using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.Infrastructure.Navigation;

/// <summary>
/// This factory is used to bundle the <see cref="NavigationState"/>
/// and <see cref="NavigationService"/> together within the same shell.
/// </summary>
public sealed class ShellContextFactory : IShellContextFactory
{
    private readonly INavigationRegistry _registry;
    private readonly IServiceProvider _services;
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger _logger;

    public ShellContextFactory(
        INavigationRegistry registry, 
        IServiceProvider services,
        IErrorHandler errorHandler,
        ILogger logger)
    {
        _registry = registry;
        _services = services;
        _errorHandler = errorHandler;
        _logger = logger;
    }

    public ShellContext Create()
    {
        var state = _services.GetRequiredService<NavigationState>();
        var tracker = _services.GetRequiredService<IInitializationTracker>();
        var navigation = new NavigationService(_services, _registry, state, tracker, _errorHandler, _logger);
        return new ShellContext(state, navigation, tracker);
    }
}