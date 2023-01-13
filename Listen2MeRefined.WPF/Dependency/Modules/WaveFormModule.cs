using Autofac;
using Listen2MeRefined.Infrastructure.Media.SoundWave;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class WaveFormModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<Canvas>()
            .As<ICanvas>()
            .AsImplementedInterfaces();
        
        builder
            .RegisterType<WaveFormDrawer>()
            .As<IWaveFormDrawer>()
            .AsImplementedInterfaces();
        
        builder
            .RegisterType<PeakProvider>()
            .As<IPeakProvider>()
            .AsImplementedInterfaces();
        
        builder
            .RegisterType<FileReader>()
            .As<IFileReader>()
            .AsImplementedInterfaces();
    }
}