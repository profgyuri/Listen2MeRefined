using Autofac;
using MediatR;

namespace Listen2MeRefined.WPF.Dependency.Modules;

internal sealed class MediatorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<Mediator>()
            .As<IMediator>()
            .InstancePerLifetimeScope();

        builder.Register<ServiceFactory>(context =>
        {
            var c = context.Resolve<IComponentContext>();
            return t => c.Resolve(t);
        });
    }
}