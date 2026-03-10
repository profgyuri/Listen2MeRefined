using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Windows;
using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.WPF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class WrappersModule
{
    internal static IHostBuilder ConfigureWrappers(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IGlobalHook, SharpHookHandler>();
            services.AddTransient<TimedTask>();
            services.AddMediatR(cfg => 
                cfg.RegisterServicesFromAssemblies([
                    typeof(App).Assembly,
                    typeof(StartupManager).Assembly,
                    typeof(SettingsWindowViewModel).Assembly
                ]));
        });
        
        return builder;
    }
}