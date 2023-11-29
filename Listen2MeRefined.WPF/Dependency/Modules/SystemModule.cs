using System.Linq;
using System.Windows.Media;
using Autofac;
using Listen2MeRefined.Core;
using Listen2MeRefined.Infrastructure;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.SystemOperations;

namespace Listen2MeRefined.WPF.Dependency.Modules;

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
            .RegisterType<FolderScannerService>()
            .As<IFolderScanner>()
            .SingleInstance();

        builder
            .RegisterType<FileScannerService>()
            .As<IFileScanner>()
            .SingleInstance();
    }
}