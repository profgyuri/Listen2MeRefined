using Autofac;
using MediatR.Extensions.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection.Builder;

namespace Listen2MeRefined.WPF.Dependency;

internal static class IocContainer
{
    private static IContainer? _container;

    internal static IContainer GetContainer()
    {
        if (_container is not null)
        {
            return _container;
        }

        var builder = new ContainerBuilder();

        var configuration = MediatRConfigurationBuilder
            .Create(typeof(IocContainer).Assembly)
            .WithAllOpenGenericHandlerTypesRegistered()
            .Build();

        builder.RegisterMediatR(configuration);
        builder.RegisterAssemblyModules(typeof(IocContainer).Assembly);

        return _container = builder.Build();
    }
}