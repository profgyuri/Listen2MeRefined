using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Views.DefaultHomeViews;
using Listen2MeRefined.WPF.Views.SettingsTabs;
using Listen2MeRefined.WPF.Views.Shells;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public class SettingsModule : IModule
{
    public string Name { get; } = "SettingsShell";
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<SettingsShellViewModel>();
        services.AddSingleton<SettingsShell>();

        services.AddTransient<SettingsGeneralTabViewModel>();
        services.AddTransient<SettingsGeneralTabView>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<SettingsGeneralTabViewModel>("general");
    }
}