using System.Linq;
using System.Windows.Media;
using Autofac;
using Listen2MeRefined.Core;
using Listen2MeRefined.Infrastructure.Data;
using Source.Storage;

namespace Listen2MeRefined.WPF.Dependency.Modules;

internal sealed class SettingsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<DatabaseSettingsManager<AppSettings>>()
            .As<ISettingsManager<AppSettings>>();
        
        builder
            .Register(_ => new FontFamilies(Fonts.SystemFontFamilies.Select(f => f.Source)))
            .SingleInstance();
    }
}