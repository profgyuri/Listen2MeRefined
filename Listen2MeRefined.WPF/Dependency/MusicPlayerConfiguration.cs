using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class MusicPlayerConfiguration
{
    internal static IHostBuilder ConfigureMusicPlayer(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IMusicPlayerController, NAudioMusicPlayer>();
            services.AddSingleton<ITrackLoader, NAudioTrackLoader>();
            services.AddSingleton<IPlaybackOutput, WaveOutPlaybackOutput>();
            services.AddSingleton<IPlaybackProgressMonitor, PlaybackProgressMonitor>();
            services.AddSingleton<IPlaylistQueue, PlaylistQueue>();
            services.AddSingleton<IPlaybackQueueService, PlaybackQueueService>();
        });
        
        return builder;
    }
}