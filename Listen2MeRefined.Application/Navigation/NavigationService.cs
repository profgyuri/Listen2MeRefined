using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Listen2MeRefined.Application.Navigation;

/// <summary>
/// Performs asynchronous VM-first navigation.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationRegistry _registry;
    private readonly NavigationState _state;
    private readonly IInitializationTracker _initializationTracker;
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    public NavigationService(
        IServiceProvider serviceProvider,
        INavigationRegistry registry,
        NavigationState state,
        IInitializationTracker initializationTracker,
        IErrorHandler errorHandler,
        ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
        _state = state;
        _initializationTracker = initializationTracker;
        _errorHandler = errorHandler;
        _logger = logger.ForContext<NavigationService>();
    }

    /// <inheritdoc />
    public bool CanNavigate(string route) => _registry.TryResolve(route, out _);

    /// <inheritdoc />
    public async Task NavigateAsync(string route, object? parameter = null, CancellationToken cancellationToken = default)
    {
        if (!_registry.TryResolve(route, out var target) || target is null)
        {
            throw new InvalidOperationException($"Route '{route}' is not registered.");
        }

        try
        {
            var viewModel = _serviceProvider.GetRequiredService(target.ViewModelType);
            if (viewModel is IInitializeAsync initializable)
            {
                await _initializationTracker.EnsureInitializedAsync(initializable, cancellationToken).ConfigureAwait(false);
            }

            _state.CurrentRoute = target.Route;
            _state.CurrentViewModel = viewModel;
        }
        catch (Exception exception)
        {
            var context = $"Navigation to route '{route}'";
            _logger.Error(exception, "Navigation failed for route {Route}.", route);
            await _errorHandler.HandleAsync(exception, context, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
