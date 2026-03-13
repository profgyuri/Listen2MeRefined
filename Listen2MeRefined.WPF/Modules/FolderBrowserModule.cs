using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Views.DefaultHomeViews;
using Listen2MeRefined.WPF.Views.Shells;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class FolderBrowserModule : IModule
{
    public string Name { get; } = "FolderBrowserShell";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<FolderBrowserShellViewModel>();
        services.AddSingleton<FolderBrowserShell>();

        services.AddTransient<FolderBrowserShellDefaultHomeViewModel>();
        services.AddTransient<FolderBrowserShellDefaultHomeView>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<FolderBrowserShellDefaultHomeViewModel>("folderBrowser/home");
    }

    public void RegisterWindows(IWindowRegistry registry)
    {
        registry.Register<FolderBrowserShellViewModel, FolderBrowserShell>();
    }
}