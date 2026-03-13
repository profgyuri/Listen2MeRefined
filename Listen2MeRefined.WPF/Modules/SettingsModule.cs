using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Application.ViewModels.Shells;
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

        services.AddTransient<SettingsAdvancedTabViewModel>();
        services.AddTransient<SettingsAdvancedTabView>();
        
        services.AddTransient<SettingsGeneralTabViewModel>();
        services.AddTransient<SettingsGeneralTabView>();
        
        services.AddTransient<SettingsHooksAndAlertsTabViewModel>();
        services.AddTransient<SettingsHooksAndAlertsTabView>();
        
        services.AddTransient<SettingsLibraryTabViewModel>();
        services.AddTransient<SettingsLibraryTabView>();
        
        services.AddTransient<SettingsPlaybackTabViewModel>();
        services.AddTransient<SettingsPlaybackTabView>();
        
        services.AddTransient<SettingsPlaylistsTabViewModel>();
        services.AddTransient<SettingsPlaylistsTabView>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<SettingsAdvancedTabViewModel>("settings/advanced");
        registry.Register<SettingsGeneralTabViewModel>("settings/general");
        registry.Register<SettingsHooksAndAlertsTabViewModel>("settings/hooksAndAlerts");
        registry.Register<SettingsLibraryTabViewModel>("settings/library");
        registry.Register<SettingsPlaybackTabViewModel>("settings/playback");
        registry.Register<SettingsPlaylistsTabViewModel>("settings/playlists");
    }
}