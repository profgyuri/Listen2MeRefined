using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class SearchResultsModule : IModule
{
    public string Name { get; } = "SearchResults";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<SearchResultsPaneViewModel>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<SearchResultsPaneViewModel>("searchResults");
    }
}