using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Views.DefaultHomeViews;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class MainShellModule : IModule
{
    public string Name { get; } = "MainShell";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<MainShellViewModel>();
        services.AddSingleton<MainShellView>();

        services.AddTransient<MainShellDefaultHomeViewViewModel>();
        services.AddTransient<MainShellDefaultHomeView>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<MainShellDefaultHomeViewViewModel>("home");
    }
}