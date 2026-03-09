using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Infrastructure.Utils;

namespace Listen2MeRefined.WPF.Dependency.Modules;
using Autofac;
using Listen2MeRefined.WPF.Utils;

public class WrappersModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<SharpHookHandler>()
            .As<IGlobalHook>()
            .SingleInstance();
        
        builder
            .Register(_ => new TimedTask())
            .InstancePerDependency();
    }
}