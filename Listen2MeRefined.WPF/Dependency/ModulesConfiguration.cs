using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Listen2MeRefined.WPF.Dependency;

public static class ModulesConfiguration
{
    internal static IHostBuilder ConfigureModules(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var provider = services.BuildServiceProvider();
            var moduleCatalogOptions = provider.GetRequiredService<ModuleCatalogOptions>();
            var navigationRegistry = provider.GetRequiredService<INavigationRegistry>();
            var windowRegistry = provider.GetRequiredService<IWindowRegistry>();
            var discoveredModules =
                ModuleCatalog.DiscoverModules(
                    moduleCatalogOptions,
                    Log.Logger.ForContext<ModuleCatalog>());
            foreach (var module in discoveredModules)
            {
                module.RegisterServices(services);
                module.RegisterNavigation(navigationRegistry);
                module.RegisterWindows(windowRegistry);
            }

            services.AddSingleton<IModuleCatalog>(new ModuleCatalog(discoveredModules));
        });
        
        return builder;
    }
}