using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.Infrastructure.Startup.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class StartupModule
{
    internal static IHostBuilder ConfigureStartup(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupManager, StartupManager>();

            services.AddSingleton<DatabaseMigrationStartupTask>();
            services.AddSingleton<IDatabaseMigrationStartupTask>(sp => sp.GetRequiredService<DatabaseMigrationStartupTask>());
            services.AddSingleton<IStartupTask>(sp => sp.GetRequiredService<DatabaseMigrationStartupTask>());

            services.AddSingleton<IStartupTask, FontFamilyStartupTask>();
            services.AddSingleton<IStartupTask, AudioOutputStartupTask>();
            services.AddSingleton<IStartupTask, FolderScanStartupTask>();
            services.AddSingleton<IStartupTask, GlobalHookStartupTask>();
            services.AddSingleton<IStartupTask, ThemeStartupTask>();
        });
        
        return builder;
    }
}