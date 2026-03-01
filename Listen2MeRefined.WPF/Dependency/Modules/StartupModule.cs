using Autofac;
using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.Infrastructure.Startup.Tasks;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class StartupModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<StartupManager>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .RegisterType<DatabaseMigrationStartupTask>()
            .AsSelf()
            .As<IDatabaseMigrationStartupTask>()
            .As<IStartupTask>()
            .SingleInstance();
        builder
            .RegisterType<FontFamilyStartupTask>()
            .AsSelf()
            .As<IStartupTask>()
            .SingleInstance();
        builder
            .RegisterType<AudioOutputStartupTask>()
            .AsSelf()
            .As<IStartupTask>()
            .SingleInstance();
        builder
            .RegisterType<FolderScanStartupTask>()
            .AsSelf()
            .As<IStartupTask>()
            .SingleInstance();
        builder
            .RegisterType<GlobalHookStartupTask>()
            .AsSelf()
            .As<IStartupTask>()
            .SingleInstance();
        builder
            .RegisterType<ThemeStartupTask>()
            .AsSelf()
            .As<IStartupTask>()
            .SingleInstance();
    }
}
