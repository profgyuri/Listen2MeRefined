namespace Listen2MeRefined.WPF.Dependency;
using Autofac;
using MediatR.Extensions.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection.Builder;

internal static class IocContainer
{
    private static IContainer? _container;
    private static ILifetimeScope? _appScope;

    internal static IContainer Container => _container ??= BuildContainer();

    internal static ILifetimeScope AppScope => _appScope ??= Container.BeginLifetimeScope("App");

    private static IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();

        var configuration = MediatRConfigurationBuilder
            .Create(typeof(IocContainer).Assembly)
            .WithAllOpenGenericHandlerTypesRegistered()
            .Build();

        builder.RegisterMediatR(configuration);
        builder.RegisterAssemblyModules(typeof(IocContainer).Assembly);

        return builder.Build();
    }

    internal static void DisposeAppScope()
    {
        _appScope?.Dispose();
        _appScope = null;

        _container?.Dispose();
        _container = null;
    }
}