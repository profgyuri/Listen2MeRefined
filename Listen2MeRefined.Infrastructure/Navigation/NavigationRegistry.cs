using System.Collections.Concurrent;
using Listen2MeRefined.Application.Navigation;

namespace Listen2MeRefined.Infrastructure.Navigation;

/// <summary>
/// Stores route-to-view model mappings.
/// </summary>
public sealed class NavigationRegistry : INavigationRegistry
{
    private readonly ConcurrentDictionary<string, NavigationTarget> _routes =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Type, NavigationTarget> _routesByViewModel = new();

    /// <inheritdoc />
    public IReadOnlyCollection<string> Routes => _routes.Keys.ToArray();

    /// <inheritdoc />
    public void Register<TViewModel>(string route) where TViewModel : class
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            throw new ArgumentException("Route must be non-empty.", nameof(route));
        }

        var normalizedRoute = route.Trim();
        var target = new NavigationTarget(normalizedRoute, typeof(TViewModel));
        if (!_routes.TryAdd(normalizedRoute, target))
        {
            throw new InvalidOperationException($"Route '{normalizedRoute}' is already registered.");
        }

        if (!_routesByViewModel.TryAdd(typeof(TViewModel), target))
        {
            _routes.TryRemove(normalizedRoute, out _);

            if (_routesByViewModel.TryGetValue(typeof(TViewModel), out var existingTarget))
            {
                throw new InvalidOperationException(
                    $"View model '{typeof(TViewModel).FullName}' is already registered to route '{existingTarget.Route}'.");
            }

            throw new InvalidOperationException(
                $"View model '{typeof(TViewModel).FullName}' is already registered to a route.");
        }
    }

    /// <inheritdoc />
    public bool TryResolve(string route, out NavigationTarget? target)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            target = null;
            return false;
        }

        return _routes.TryGetValue(route.Trim(), out target);
    }

    /// <inheritdoc />
    public bool TryResolve<TViewModel>(out NavigationTarget? target) where TViewModel : class =>
        _routesByViewModel.TryGetValue(typeof(TViewModel), out target);
}
