using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.SettingsTabs;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Infrastructure.Navigation;
using Listen2MeRefined.WPF.Modules;
using Listen2MeRefined.WPF.Utils.Navigation;
using Listen2MeRefined.WPF.Views.Shells;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.Tests.Modules;

public sealed class SettingsModuleTests
{
    [Fact]
    public void RegisterNavigation_State_RegistersAllSettingsRoutes()
    {
        var module = new SettingsModule();
        var registry = new NavigationRegistry();

        module.RegisterNavigation(registry);

        AssertRoute<SettingsGeneralTabViewModel>(registry, "settings/general");
        AssertRoute<SettingsPlaybackTabViewModel>(registry, "settings/playback");
        AssertRoute<SettingsLibraryTabViewModel>(registry, "settings/library");
        AssertRoute<SettingsPlaylistsTabViewModel>(registry, "settings/playlists");
        AssertRoute<SettingsHooksAndAlertsTabViewModel>(registry, "settings/hooksAndAlerts");
        AssertRoute<SettingsAdvancedTabViewModel>(registry, "settings/advanced");
    }

    [Fact]
    public void RegisterWindows_State_RegistersSettingsShellWindow()
    {
        var module = new SettingsModule();
        var registry = new WindowRegistry();

        module.RegisterWindows(registry);

        var resolvedType = registry.Resolve<SettingsShellViewModel>();
        Assert.Equal(typeof(SettingsShell), resolvedType);
    }

    [Fact]
    public void RegisterServices_State_RegistersSettingsNavigationProvider()
    {
        var module = new SettingsModule();
        var services = new ServiceCollection();

        module.RegisterServices(services);

        var descriptor = Assert.Single(
            services,
            x => x.ServiceType == typeof(ISettingsShellNavigationProvider));
        Assert.Equal(typeof(SettingsShellNavigationProvider), descriptor.ImplementationType);
    }

    private static void AssertRoute<TViewModel>(NavigationRegistry registry, string route)
        where TViewModel : class
    {
        Assert.True(registry.TryResolve(route, out var target));
        Assert.NotNull(target);
        Assert.Equal(typeof(TViewModel), target!.ViewModelType);
    }
}
