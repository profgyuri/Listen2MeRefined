using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Infrastructure.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class WrappersConfiguration
{
    internal static IHostBuilder ConfigureWrappers(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IGlobalHook, SharpHookHandler>();
            services.AddTransient<TimedTask>();
        });
        
        return builder;
    }
}