using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Listen2MeRefined.Infrastructure.Playlist;
using Listen2MeRefined.WPF.Views.Widgets;
using Microsoft.Extensions.DependencyInjection;
using NAudio.Wave;
using SkiaSharp;

namespace Listen2MeRefined.WPF.Modules;

public sealed class PlaybackModule : IModule
{
    public string Name { get; } = "Playback";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMusicPlayerController, NAudioMusicPlayer>();
        services.AddSingleton<ITrackLoader, NAudioTrackLoader>();
        services.AddSingleton<IPlaybackOutput, WaveOutPlaybackOutput>();
        services.AddSingleton<IPlaybackProgressMonitor, PlaybackProgressMonitor>();
        services.AddSingleton<IPlaylistQueue, PlaylistQueue>();
        services.AddSingleton<IPlaybackQueueService, PlaybackQueueService>();
        services.AddSingleton<IOutputDevice, NAudioOutputDevices>();

        services.AddSingleton<IPlaylistQueueState, PlaylistQueueState>();
        services.AddSingleton<IPlaylistQueueRoutingService, PlaylistQueueRoutingService>();
        services.AddSingleton<IPlaybackQueueActionsService, PlaybackQueueActionsService>();
        services.AddSingleton<IPlaybackContextSyncService, PlaybackContextSyncService>();
        services.AddTransient<IPlaybackVolumeSetter, PlaybackVolumeSetter>();

        services.AddSingleton<SkiaCanvas>();
        services.AddSingleton<ICanvas<SKPoint, SKBitmap>>(sp => sp.GetRequiredService<SkiaCanvas>());
        services.AddSingleton<IWaveformPaletteAware>(sp => sp.GetRequiredService<SkiaCanvas>());
        services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<SkiaCanvas>());
        services.AddSingleton<IWaveFormDrawer<SKBitmap>, WaveFormDrawer>();
        services.AddSingleton<IWaveformViewportPolicy, WaveformViewportPolicy>();
        services.AddTransient<IWaveformResizeScheduler, WaveformResizeScheduler>();
        services.AddTransient<IWaveformRenderer, WaveformRenderer>();
        services.AddTransient<IPeakProvider<ISampleProvider>, PeakProvider>();
        services.AddTransient<IFileReader<ISampleProvider>, FileReader>();

        services.AddTransient<TrackInfoViewModel>();
        services.AddTransient<TrackInfoView>();
        services.AddTransient<PlaybackControlsViewModel>();
        services.AddTransient<PlaybackControlsView>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
    }
}
