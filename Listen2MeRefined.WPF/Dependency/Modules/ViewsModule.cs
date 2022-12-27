using Autofac;

namespace Listen2MeRefined.WPF.Dependency.Modules;

internal sealed class ViewsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register all views via assembly scanning
        builder.RegisterAssemblyTypes(typeof(MainWindow).Assembly)
            .Where(t => t.Name.EndsWith("Window"))
            .AsSelf()
            .SingleInstance();
    }
}