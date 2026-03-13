using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Views.DefaultHomeViews;
using Listen2MeRefined.WPF.Views.Shells;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class AdvancedSearchModule : IModule
{
    public string Name { get; } = "AdvancedSearchShell";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<AdvancedSearchShellViewModel>();
        services.AddSingleton<AdvancedSearchShell>();

        services.AddTransient<AdvancedSearchShellDefaultHomeViewModel>();
        services.AddTransient<AdvancedSearchShellDefaultHomeView>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<AdvancedSearchShellDefaultHomeViewModel>("advancedSearch/home");
    }
}