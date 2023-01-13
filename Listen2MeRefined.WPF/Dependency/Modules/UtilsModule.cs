using Autofac;
using Listen2MeRefined.Infrastructure.Data;
using Source.Storage;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class UtilsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<DatabaseSettingsManager<AppSettings>>()
            .As<ISettingsManager<AppSettings>>();
    }
}