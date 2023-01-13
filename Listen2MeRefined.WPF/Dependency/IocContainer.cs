using Autofac;

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

        builder.RegisterAssemblyModules(typeof(IocContainer).Assembly);

        return _container = builder.Build();
    }
}