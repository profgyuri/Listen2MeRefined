using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class PlaybackControlsModule : IModule
{
    public string Name { get; } = "PlaybackControls";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<PlaybackControlsViewModel>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<PlaybackControlsViewModel>("playbackControls");
    }
}