using Autofac;

namespace Listen2MeRefined.WPF.Dependency;

internal static class AutofacContainer
{
    private static IContainer? _container;
    
    public static IContainer Container
    {
        get
        {
            if (_container == null)
            {
                var builder = new ContainerBuilder();
                
                // Register all modules via assembly scanning
                builder.RegisterAssemblyModules(typeof(AutofacContainer).Assembly);
                
                _container = builder.Build();
            }
    
            return _container;
        }
    }
}