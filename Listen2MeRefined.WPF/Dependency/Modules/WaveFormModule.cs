using Autofac;
using Listen2MeRefined.Infrastructure.Media.SoundWave;
using NAudio.Wave;
using SkiaSharp;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class WaveFormModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<SkiaCanvas>()
            .As<ICanvas<SKPoint, SKBitmap>>()
            .AsImplementedInterfaces();
        
        builder
            .RegisterType<WaveFormDrawer>()
            .As<IWaveFormDrawer<SKBitmap>>()
            .AsImplementedInterfaces()
            .SingleInstance();
        
        builder
            .RegisterType<PeakProvider>()
            .As<IPeakProvider<ISampleProvider>>()
            .AsImplementedInterfaces();
        
        builder
            .RegisterType<FileReader>()
            .As<IFileReader<ISampleProvider>>()
            .AsImplementedInterfaces();
    }
}