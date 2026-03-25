using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Infrastructure.Startup.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public sealed class StartupPipelineModule : IModule
{
    public string Name { get; } = "StartupPipeline";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<DatabaseMigrationStartupTask>();
        services.AddSingleton<IDatabaseMigrationStartupTask>(sp => sp.GetRequiredService<DatabaseMigrationStartupTask>());
        services.AddSingleton<IStartupTask>(sp => sp.GetRequiredService<DatabaseMigrationStartupTask>());

        services.AddSingleton<IStartupTask, FolderScanStartupTask>();
        services.AddSingleton<IStartupTask, GlobalHookStartupTask>();
        services.AddSingleton<IStartupTask, ThemeStartupTask>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
    }
}
