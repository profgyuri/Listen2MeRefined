using Autofac;
using Listen2MeRefined.Core.Source;
using Listen2MeRefined.Infrastructure;
using Listen2MeRefined.Infrastructure.Media;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class WrappersModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<WindowsMusicPlayer>()
            .AsImplementedInterfaces()
            .SingleInstance();

        builder
            .RegisterType<GmaGlobalHookHandler>()
            .As<IGlobalHook>()
            .SingleInstance();
        
        builder
            .Register(_ => new TimedTask())
            .InstancePerDependency();
    }
}