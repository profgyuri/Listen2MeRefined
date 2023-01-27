using Autofac;
using Listen2MeRefined.WPF.Views;
using Listen2MeRefined.WPF.Views.Pages;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class ViewsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MainWindow>().SingleInstance();
        builder.RegisterType<FolderBrowserWindow>().InstancePerLifetimeScope();
        builder.RegisterType<AdvancedSearchWindow>().InstancePerLifetimeScope();
        builder.RegisterType<SettingsWindow>().InstancePerLifetimeScope();
        builder.RegisterType<NewSongWindow>().InstancePerDependency();
        builder.RegisterType<CurrentlyPlayingPage>().InstancePerDependency();
    }
}