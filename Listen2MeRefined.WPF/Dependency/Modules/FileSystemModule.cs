using Autofac;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.SystemOperations;

namespace Listen2MeRefined.WPF.Dependency.Modules;

internal sealed class FileSystemModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<FolderBrowser>()
            .As<IFolderBrowser>();
        
        builder
            .RegisterType<FileEnumerator>()
            .As<IFileEnumerator>();
        
        builder
            .RegisterType<FolderScannerService>()
            .As<IFolderScanner>()
            .SingleInstance();
        
        builder
            .RegisterType<SoundFileAnalyzer>()
            .As<IFileAnalyzer<AudioModel>>();
    }
}