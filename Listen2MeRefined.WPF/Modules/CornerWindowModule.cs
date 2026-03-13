using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Views.DefaultHomeViews;
using Listen2MeRefined.WPF.Views.Shells;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class CornerWindowModule : IModule
{
    public string Name { get; } = "CornerWindowShell";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<CornerWindowShellViewModel>();
        services.AddSingleton<CornerWindowShell>();

        services.AddTransient<CornerShellDefaultHomeViewModel>();
        services.AddTransient<CornerShellDefaultHomeView>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<CornerShellDefaultHomeViewModel>("corner/home");
    }

    public void RegisterWindows(IWindowRegistry registry)
    {
        registry.Register<CornerWindowShellViewModel, CornerWindowShell>();
    }
}