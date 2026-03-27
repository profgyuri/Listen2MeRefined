using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Scanning.Files;
using Listen2MeRefined.Infrastructure.Scanning.Folders;
using Listen2MeRefined.Infrastructure.Searching;
using Listen2MeRefined.WPF.Services;
using Listen2MeRefined.WPF.Views.DefaultHomeViews;
using Listen2MeRefined.WPF.Views.Shells;
using Listen2MeRefined.WPF.Views.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public sealed class SearchModule : IModule
{
    public string Name { get; } = "Search";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<AudioRepository>();
        services.AddTransient<IAudioRepository>(ctx => ctx.GetRequiredService<AudioRepository>());
        services.AddTransient<IAdvancedDataReader<AdvancedFilter, AudioModel>>(ctx => ctx.GetRequiredService<AudioRepository>());
        services.AddTransient<IFromFolderRemover>(ctx => ctx.GetRequiredService<AudioRepository>());
        services.AddTransient<IRepository<AudioModel>>(ctx => ctx.GetRequiredService<AudioRepository>());

        services.AddTransient<IFolderBrowser, FolderBrowser>();
        services.AddTransient<IFileAnalyzer<AudioModel>, SoundFileAnalyzer>();
        services.AddTransient<IFileEnumerator, FileEnumerator>();
        services.AddSingleton<IFolderScanner, FolderScanner>();
        services.AddSingleton<IFileScanner, FileScanner>();
        services.AddSingleton<IClipboardService, WpfClipboardService>();

        services.AddTransient<IFolderNavigationService, FolderNavigationService>();
        services.AddTransient<IPinnedFoldersService, PinnedFoldersService>();
        services.AddTransient<IAdvancedSearchCriteriaService, AdvancedSearchCriteriaService>();
        services.AddTransient<IAudioSearchExecutionService, AudioSearchExecutionService>();
        services.AddTransient<ISearchResultsTransferService, SearchResultsTransferService>();

        services.AddTransient<SearchbarViewModel>();
        services.AddTransient<SearchBarView>();
        services.AddTransient<SearchResultsPaneViewModel>();
        services.AddTransient<SearchResultsPaneView>();

        services.AddTransient<AdvancedSearchShellViewModel>();
        services.AddTransient<AdvancedSearchShell>();
        services.AddTransient<AdvancedSearchShellDefaultHomeViewModel>();
        services.AddTransient<AdvancedSearchShellDefaultHomeView>();

        services.AddTransient<FolderBrowserShellViewModel>();
        services.AddTransient<FolderBrowserShell>();
        services.AddTransient<FolderBrowserShellDefaultHomeViewModel>();
        services.AddTransient<FolderBrowserShellDefaultHomeView>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<AdvancedSearchShellDefaultHomeViewModel>("advancedSearch/home");
        registry.Register<FolderBrowserShellDefaultHomeViewModel>("folderBrowser/home");
    }

    public void RegisterWindows(IWindowRegistry registry)
    {
        registry.Register<AdvancedSearchShellViewModel, AdvancedSearchShell>();
        registry.Register<FolderBrowserShellViewModel, FolderBrowserShell>();
    }
}
