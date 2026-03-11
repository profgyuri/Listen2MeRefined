using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class ListsModule : IModule
{
    public string Name { get; } = "Lists";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ListsViewModel>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<ListsViewModel>("lists");
    }
}