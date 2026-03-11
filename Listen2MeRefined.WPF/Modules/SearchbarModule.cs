using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class SearchbarModule : IModule
{
    public string Name { get; } = "Searchbar";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<SearchbarViewModel>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<SearchbarViewModel>("searchbar");
    }
}