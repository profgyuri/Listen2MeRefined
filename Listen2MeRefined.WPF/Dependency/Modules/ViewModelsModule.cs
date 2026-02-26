using Autofac;
using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.Infrastructure.ViewModels;
using Listen2MeRefined.Infrastructure.ViewModels.MainWindow;

namespace Listen2MeRefined.WPF.Dependency.Modules;

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
        builder
            .RegisterType<AdvancedSearchViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
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
            .RegisterType<PlaybackControlsViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .RegisterType<ListsViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .RegisterType<PlaylistPaneViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .RegisterType<SearchResultsPaneViewModel>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
        builder
            .RegisterType<StartupManager>()
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
    }
}
