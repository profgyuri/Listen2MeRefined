using Autofac;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class MusicPlayerModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<NAudioMusicPlayer>()
            .As<IMusicPlayerController>()
            .SingleInstance();
        
        builder
            .RegisterType<Playlist>()
            .As<IPlaylist>()
            .SingleInstance();
    }
}