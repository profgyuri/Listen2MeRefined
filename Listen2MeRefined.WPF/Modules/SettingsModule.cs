using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.WPF.Utils.Theming;
using Listen2MeRefined.WPF.Views.SettingsTabs;
using Listen2MeRefined.WPF.Views.Shells;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Windows.Media;

namespace Listen2MeRefined.WPF.Modules;

public class SettingsModule : IModule
{
    public string Name { get; } = "Settings";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsManager<AppSettings>, DatabaseSettingsManager<AppSettings>>();
        services.AddTransient<IAppSettingsReader, AppSettingsReader>();
        services.AddTransient<IAppSettingsWriter, AppSettingsWriter>();
        services.AddTransient<IGlobalHookSettingsSyncService, GlobalHookSettingsSyncService>();
        services.AddTransient<IPlaybackDefaultsService, PlaybackDefaultsService>();
        services.AddTransient<IWindowPositionPolicyService, WindowPositionPolicyService>();
        services.AddSingleton<IAppThemeService, AppThemeService>();
        services.AddSingleton(_ =>
            new FontFamilies(Fonts.SystemFontFamilies
                .Select(f => f.Source)
                .OrderBy(f => f)));

        services.AddSingleton<ISettingsShellNavigationProvider, SettingsShellNavigationProvider>();
        services.AddTransient<SettingsShellViewModel>();
        services.AddTransient<SettingsShell>();

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

    public void RegisterWindows(IWindowRegistry registry)
    {
        registry.Register<SettingsShellViewModel, SettingsShell>();
    }
}
