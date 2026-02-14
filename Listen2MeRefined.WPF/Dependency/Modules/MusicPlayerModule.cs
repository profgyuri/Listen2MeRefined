using Autofac;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class MusicPlayerModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<NAudioMusicPlayer>()
            .As<IMusicPlayerController>()
            .As<INotificationHandler<AudioOutputDeviceChangedNotification>>()
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