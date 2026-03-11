using Listen2MeRefined.Application.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Listen2MeRefined.WPF.Dependency;

public static class ModulesModule
{
    internal static IHostBuilder ConfigureModules(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var moduleCatalogOptions = services.BuildServiceProvider().GetRequiredService<ModuleCatalogOptions>();
            var discoveredModules =
                ModuleCatalog.DiscoverModules(
                    moduleCatalogOptions,
                    Log.Logger.ForContext<ModuleCatalog>());
            foreach (var module in discoveredModules)
            {
                module.RegisterServices(services);
            }

            services.AddSingleton<IModuleCatalog>(new ModuleCatalog(discoveredModules));
        });
        
        return builder;
    }
}