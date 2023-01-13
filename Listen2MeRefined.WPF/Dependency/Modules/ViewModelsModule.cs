using Autofac;

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
    }
}