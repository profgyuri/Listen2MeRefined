using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.Application.Modules;

/// <summary>
/// Defines a composable MVVM module.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the unique module name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers module services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    void RegisterServices(IServiceCollection services);

    /// <summary>
    /// Registers module navigation routes.
    /// </summary>
    /// <param name="registry">The navigation registry.</param>
    void RegisterNavigation(INavigationRegistry registry);

    /// <summary>
    /// Pairs module windows with their view models.
    /// </summary>
    /// <param name="registry">The window registry.</param>   
    void RegisterWindows(IWindowRegistry registry) { }
}
