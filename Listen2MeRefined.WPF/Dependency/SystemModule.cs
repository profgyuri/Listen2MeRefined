using System.Linq;
using System.Windows.Media;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Scanning.Files;
using Listen2MeRefined.Infrastructure.Scanning.Folders;
using Listen2MeRefined.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class SystemModule
{
    internal static IHostBuilder ConfigureSystem(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddTransient<IFolderBrowser, FolderBrowser>();
            services.AddTransient<IFileAnalyzer<AudioModel>, SoundFileAnalyzer>();
            services.AddTransient<IFileEnumerator, FileEnumerator>();
            services.AddSingleton(_ => new FontFamilies(Fonts.SystemFontFamilies.Select(f => f.Source)));
            services.AddSingleton<IFolderScanner, FolderScanner>();
            services.AddSingleton<IFileScanner, FileScanner>();
            services.AddSingleton<IClipboardService, WpfClipboardService>();
            services.AddSingleton<IOutputDevice, NAudioOutputDevices>();
        });
        
        return builder;
    }
}