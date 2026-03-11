using System.Collections.Concurrent;

namespace Listen2MeRefined.Application.Navigation;

/// <summary>
/// Stores route-to-view model mappings.
/// </summary>
public sealed class NavigationRegistry : INavigationRegistry
{
    private readonly ConcurrentDictionary<string, NavigationTarget> _routes =
        new(StringComparer.OrdinalIgnoreCase);

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
}
