using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Infrastructure.Settings;
using Listen2MeRefined.WPF.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.Tests.Modules;

public sealed class FinalModulesRegistrationTests
{
    [Fact]
    public void RegisterServices_State_RegistersExpectedServiceLifetimes()
    {
        var services = new ServiceCollection();

        foreach (var module in CreateFinalModules())
        {
            module.RegisterServices(services);
        }

        AssertLifetime<IWindowManager>(services, ServiceLifetime.Singleton);
        AssertLifetime<IBackgroundTaskStatusService>(services, ServiceLifetime.Singleton);
        AssertLifetime<IMusicPlayerController>(services, ServiceLifetime.Singleton);
        AssertLifetime<ISettingsManager<AppSettings>>(services, ServiceLifetime.Singleton);
        AssertLifetime<IAppUpdateChecker>(services, ServiceLifetime.Transient);
        AssertLifetime<IStartupManager>(services, ServiceLifetime.Singleton);
        AssertLifetime<MainShellViewModel>(services, ServiceLifetime.Transient);
        AssertLifetime<AdvancedSearchShellViewModel>(services, ServiceLifetime.Transient);
        AssertLifetime<FolderBrowserShellViewModel>(services, ServiceLifetime.Transient);
        AssertLifetime<SettingsShellViewModel>(services, ServiceLifetime.Transient);
    }

    [Fact]
    public void RegisterServices_State_SecondaryShellViewModelResolvesAsDistinctInstances()
    {
        var services = new ServiceCollection();

        foreach (var module in CreateFinalModules())
        {
            module.RegisterServices(services);
        }

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<FolderBrowserShellViewModel>();
        var second = provider.GetRequiredService<FolderBrowserShellViewModel>();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void ModuleCatalog_State_ReturnsFinalBoundaryModuleSet()
    {
        var catalog = new ModuleCatalog(CreateFinalModules());

        var discoveredNames = catalog.LoadModules()
            .Select(module => module.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["Infrastructure", "Playback", "Playlist", "Search", "Settings", "Shell", "Versioning"],
            discoveredNames);
    }

    private static IReadOnlyList<IModule> CreateFinalModules() =>
    [
        new InfrastructureModule(),
        new PlaybackModule(),
        new PlaylistModule(),
        new SearchModule(),
        new SettingsModule(),
        new ShellModule(),
        new VersioningModule()
    ];

    private static void AssertLifetime<TService>(IServiceCollection services, ServiceLifetime expectedLifetime)
    {
        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(TService));
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }
}
