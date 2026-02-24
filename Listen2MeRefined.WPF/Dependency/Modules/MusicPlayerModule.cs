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
            .AsImplementedInterfaces()
            .SingleInstance();

        builder
            .RegisterType<NAudioTrackLoader>()
            .As<ITrackLoader>()
            .SingleInstance();

        builder
            .RegisterType<WaveOutPlaybackOutput>()
            .As<IPlaybackOutput>()
            .SingleInstance();

        builder
            .RegisterType<PlaybackProgressMonitor>()
            .As<IPlaybackProgressMonitor>()
            .SingleInstance();
        
        builder
            .RegisterType<Playlist>()
            .As<IPlaylist>()
            .SingleInstance();
        
        builder
            .RegisterType<PlaybackQueueService>()
            .As<IPlaybackQueueService>()
            .SingleInstance();
    }
}