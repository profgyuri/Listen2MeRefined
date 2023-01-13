using Autofac;
using Listen2MeRefined.Infrastructure.Media;
using SkiaSharp;
using Source;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class WrappersModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<WindowsMusicPlayer>()
            .As<IMediaController<SKBitmap>>()
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