using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NAudio.Wave;
using SkiaSharp;

namespace Listen2MeRefined.WPF.Dependency;

public static class WaveFormConfiguration
{
    internal static IHostBuilder ConfigureWaveForm(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<SkiaCanvas>();
            services.AddSingleton<ICanvas<SKPoint, SKBitmap>>(sp => sp.GetRequiredService<SkiaCanvas>());
            services.AddSingleton<IWaveformPaletteAware>(sp => sp.GetRequiredService<SkiaCanvas>());
            services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<SkiaCanvas>());
            
            services.AddSingleton<IWaveFormDrawer<SKBitmap>, WaveFormDrawer>();
            services.AddTransient<IPeakProvider<ISampleProvider>, PeakProvider>();
            services.AddTransient<IFileReader<ISampleProvider>, FileReader>();
        });
        
        return builder;
    }
}