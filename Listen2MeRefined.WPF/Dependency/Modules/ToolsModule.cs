using Autofac;
using Listen2MeRefined.Infrastructure.Media;
using Source;

namespace Listen2MeRefined.WPF.Dependency.Modules;

internal sealed class ToolsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<WindowsMusicPlayer>()
            .As<IMediaController>()
            .As<IPlaylistReference>()
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