namespace Listen2MeRefined.WPF.Dependency.Modules;
using Autofac;

public class ViewModelsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<MainWindowViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        builder.RegisterType<FolderBrowserViewModel>();
        builder.RegisterType<AdvancedSearchViewModel>();
        builder.RegisterType<NewSongWindowViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .RegisterType<SettingsWindowViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();

        builder
            .RegisterType<SearchbarViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<PlayerControlsViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<ListsViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        builder
            .RegisterType<StartupManager>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
    }
}