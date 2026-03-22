using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Popups;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Views.Popups;
using Listen2MeRefined.WPF.Views.Shells;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class PopupModule : IModule
{
    public string Name { get; } = "PopupShell";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<PopupShell>();
        services.AddTransient<PopupShellViewModel>();

        services.AddTransient<SongDroppedPopup>();
        services.AddTransient<SongDroppedPopupViewModel>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<SongDroppedPopupViewModel>("popup/songDropped");
    }
}