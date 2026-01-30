namespace Listen2MeRefined.WPF.Dependency.Modules;
using Autofac;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Storage;
using Listen2MeRefined.Infrastructure.Versioning;
using Listen2MeRefined.WPF.Utils;
using System.Windows;

public class UtilsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<DatabaseSettingsManager<AppSettings>>()
            .As<ISettingsManager<AppSettings>>();

        builder
            .RegisterType<VersionChecker>()
            .As<IVersionChecker>();

        builder.Register(ctx =>
            new WpfUiDispatcher(Application.Current.Dispatcher))
               .As<IUiDispatcher>()
               .SingleInstance();
    }
}