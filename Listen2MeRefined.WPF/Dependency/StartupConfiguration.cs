using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.WPF.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class StartupConfiguration
{
    internal static IHostBuilder ConfigureStartup(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupManager, StartupManager>();
            services.AddHostedService<StartupHostedService>();
        });
        
        return builder;
    }
}
