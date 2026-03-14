using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.WPF.Views.DefaultHomeViews;
using Listen2MeRefined.WPF.Views.Shells;
using Listen2MeRefined.WPF.Views.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class MainModule : IModule
{
    public string Name { get; } = "MainShell";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<MainShellViewModel>();
        services.AddSingleton<MainShell>();

        services.AddTransient<MainShellDefaultHomeViewModel>();
        services.AddTransient<MainShellDefaultHomeView>();
        
        services.AddTransient<TrackInfoViewModel>();
        services.AddTransient<TrackInfoView>();
        
        services.AddTransient<SearchbarViewModel>();
        services.AddTransient<SearchBarView>();
        
        services.AddTransient<SearchResultsPaneViewModel>();
        services.AddTransient<SearchResultsPaneView>();
        
        services.AddTransient<PlaylistPaneViewModel>();
        services.AddTransient<PlaylistPaneView>();
        
        services.AddTransient<PlaybackControlsViewModel>();
        services.AddTransient<PlaybackControlsView>();
        
        services.AddTransient<ListsViewModel>();
        services.AddTransient<MainTabsView>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<MainShellDefaultHomeViewModel>("main/home");
    }

    public void RegisterWindows(IWindowRegistry registry)
    {
        registry.Register<MainShellViewModel, MainShell>();
    }
}