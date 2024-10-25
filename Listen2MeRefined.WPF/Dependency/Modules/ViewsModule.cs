namespace Listen2MeRefined.WPF.Dependency.Modules;
using Autofac;
using Listen2MeRefined.WPF.Views;

public class ViewsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MainWindow>().SingleInstance();
        builder.RegisterType<FolderBrowserWindow>().InstancePerLifetimeScope();
        builder.RegisterType<AdvancedSearchWindow>().InstancePerLifetimeScope();
        builder.RegisterType<SettingsWindow>().InstancePerLifetimeScope();
        builder.RegisterType<NewSongWindow>().SingleInstance();
    }
}