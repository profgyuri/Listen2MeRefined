namespace Listen2MeRefined.Application.Navigation;

/// <summary>
/// Defines route registration and lookup for view model navigation.
/// </summary>
public interface INavigationRegistry
{
    /// <summary>
    /// Registers a route for a view model type.
    /// </summary>
    /// <typeparam name="TViewModel">The registered view model type.</typeparam>
    /// <param name="route">The unique route key.</param>
    void Register<TViewModel>(string route) where TViewModel : class;

    /// <summary>
    /// Tries to resolve a route.
    /// </summary>
    /// <param name="route">The route key.</param>
    /// <param name="target">When this method returns, contains the navigation target when found.</param>
    /// <returns><see langword="true"/> if a route mapping exists; otherwise, <see langword="false"/>.</returns>
    bool TryResolve(string route, out NavigationTarget? target);

    /// <summary>
    /// Gets the currently registered route keys.
    /// </summary>
    IReadOnlyCollection<string> Routes { get; }
}
