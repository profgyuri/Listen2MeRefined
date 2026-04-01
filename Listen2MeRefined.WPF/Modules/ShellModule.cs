using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Popups;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.Infrastructure.Startup.Tasks;
using Listen2MeRefined.WPF.Startup;
using Listen2MeRefined.WPF.Views.DefaultHomeViews;
using Listen2MeRefined.WPF.Views.Popups;
using Listen2MeRefined.WPF.Views.Shells;
using Listen2MeRefined.WPF.Views.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Modules;

public sealed class ShellModule : IModule
{
    public string Name { get; } = "Shell";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<MainShellViewModel>();
        services.AddTransient<MainShell>();
        services.AddTransient<MainShellDefaultHomeViewModel>();
        services.AddTransient<MainShellDefaultHomeView>();
        services.AddTransient<MainHomeContentToggleViewModel>();
        services.AddTransient<MainHomeContentToggleView>();

        services.AddTransient<PopupShell>();
        services.AddTransient<PopupShellViewModel>();
        services.AddTransient<SongDroppedPopup>();
        services.AddTransient<SongDroppedPopupViewModel>();
        services.AddTransient<CrashReportPopup>();
        services.AddTransient<CrashReportWindow>();

        services.AddTransient<CornerWindowShellViewModel>();
        services.AddTransient<CornerWindowShell>();
        services.AddTransient<CornerShellDefaultHomeViewModel>();

        services.AddSingleton<IStartupManager, StartupManager>();
        services.AddHostedService<StartupHostedService>();

        services.AddSingleton<DatabaseMigrationStartupTask>();
        services.AddSingleton<IDatabaseMigrationStartupTask>(sp => sp.GetRequiredService<DatabaseMigrationStartupTask>());
        services.AddSingleton<IStartupTask>(sp => sp.GetRequiredService<DatabaseMigrationStartupTask>());

        services.AddSingleton<IStartupTask, FolderScanStartupTask>();
        services.AddSingleton<IStartupTask, GlobalHookStartupTask>();
        services.AddSingleton<IStartupTask, ThemeStartupTask>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register<MainShellDefaultHomeViewModel>("main/home");
        registry.Register<SongDroppedPopupViewModel>("popup/songDropped");
        registry.Register<CornerShellDefaultHomeViewModel>("corner/home");
    }

    public void RegisterWindows(IWindowRegistry registry)
    {
        registry.Register<MainShellViewModel, MainShell>();
        registry.Register<PopupShellViewModel, PopupShell>();
        registry.Register<CornerWindowShellViewModel, CornerWindowShell>();
    }
}
