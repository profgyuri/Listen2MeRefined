using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Scanning.Files;
using Listen2MeRefined.Infrastructure.Scanning.Folders;
using Listen2MeRefined.Infrastructure.Utils;

namespace Listen2MeRefined.WPF.Dependency.Modules;
using System.Linq;
using System.Windows.Media;
using Autofac;
using Listen2MeRefined.Infrastructure.Data.Models;

public class SystemModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<FolderBrowser>()
            .As<IFolderBrowser>();

        builder
            .RegisterType<SoundFileAnalyzer>()
            .As<IFileAnalyzer<AudioModel>>();

        builder
            .RegisterType<FileEnumerator>()
            .As<IFileEnumerator>();

        builder
            .Register(_ => new FontFamilies(Fonts.SystemFontFamilies.Select(f => f.Source)))
            .SingleInstance();

        builder
            .RegisterType<FolderScanner>()
            .As<IFolderScanner>()
            .SingleInstance();

        builder
            .RegisterType<FileScanner>()
            .As<IFileScanner>()
            .SingleInstance();
        
        builder
            .RegisterType<NAudioOutputDevices>()
            .As<IOutputDevice>()
            .SingleInstance();
    }
}