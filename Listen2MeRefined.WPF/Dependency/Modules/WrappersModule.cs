namespace Listen2MeRefined.WPF.Dependency.Modules;
using Autofac;
using Listen2MeRefined.Infrastructure;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.WPF.Utils;

public class WrappersModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<WindowsMusicPlayer>()
            .AsImplementedInterfaces()
            .SingleInstance();

        builder
            .RegisterType<SharpHookHandler>()
            .As<IGlobalHook>()
            .SingleInstance();
        
        builder
            .Register(_ => new TimedTask())
            .InstancePerDependency();
    }
}