using Autofac;

namespace Listen2MeRefined.WPF.Dependency.Modules;

internal sealed class ViewModelsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register all view models via assembly scanning
        builder.RegisterAssemblyTypes(typeof(MainWindowViewModel).Assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
    }
}