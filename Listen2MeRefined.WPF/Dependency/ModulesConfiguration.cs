using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Listen2MeRefined.WPF.Dependency;

public static class ModulesConfiguration
{
    internal static IHostBuilder ConfigureModuleServices(this IHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            var moduleCatalogOptions = context.Configuration
                .GetSection("Modules")
                .Get<ModuleCatalogOptions>() ?? new ModuleCatalogOptions();

            var discoveredModules = ModuleCatalog.DiscoverModules(
                moduleCatalogOptions,
                Log.Logger.ForContext<ModuleCatalog>());

            // Only register services here — no registry calls yet
            foreach (var module in discoveredModules)
                module.RegisterServices(services);

            services.AddSingleton<IModuleCatalog>(new ModuleCatalog(discoveredModules));
        });

        return builder;
    }

    internal static IHost ConfigureModuleNavigation(this IHost host)
    {
        var catalog = host.Services.GetRequiredService<IModuleCatalog>();
        var navigationRegistry = host.Services.GetRequiredService<INavigationRegistry>();
        var windowRegistry = host.Services.GetRequiredService<IWindowRegistry>();

        foreach (var module in catalog.LoadModules())
        {
            module.RegisterNavigation(navigationRegistry);
            module.RegisterWindows(windowRegistry);
        }

        return host;
    }
}