using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Infrastructure.Navigation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class NavigationModule
{
    internal static IHostBuilder ConfigureNavigation(this IHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            services.Configure<NavigationOptions>(context.Configuration.GetSection("Navigation"));

            var moduleCatalogOptions = context.Configuration
                .GetSection("Modules")
                .Get<ModuleCatalogOptions>() ?? new ModuleCatalogOptions();
            services.AddSingleton(moduleCatalogOptions);
            
            services.AddSingleton<INavigationRegistry, NavigationRegistry>();
            services.AddSingleton<NavigationState>();
            services.AddSingleton<IInitializationTracker, InitializationTracker>();
            services.AddSingleton<INavigationService, NavigationService>();
        });
        
        return builder;
    }
}