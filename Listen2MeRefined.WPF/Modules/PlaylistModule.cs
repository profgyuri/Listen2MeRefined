using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class PlaylistModule : IModule
{
    public string Name { get; } = "Playlist";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<PlaylistPaneViewModel>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<PlaylistPaneViewModel>("playlist");
    }
}