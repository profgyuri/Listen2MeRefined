using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Dapper;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Navigation;
using Listen2MeRefined.WPF.ErrorHandling;
using Listen2MeRefined.WPF.Utils.Navigation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using System.Data;
using AppLoggerConfiguration = Listen2MeRefined.WPF.Dependency.LoggerConfiguration;

namespace Listen2MeRefined.WPF.Modules;

public sealed class InfrastructureModule : IModule
{
    public string Name { get; } = "Infrastructure";

    public void RegisterServices(IServiceCollection services)
    {
        var logLocationService = new LocalAppDataLogLocationService();
        logLocationService.EnsureLogDirectoryExists();

        var logger = Log.Logger as Logger ?? AppLoggerConfiguration.CreateLogger(logLocationService);
        Log.Logger = logger;

        services.AddSingleton<ILogLocationService>(logLocationService);
        services.AddSingleton<ILogger>(logger);

        services.AddSingleton<ICrashDialogService, CrashDialogService>();
        services.AddSingleton<IErrorHandler, CrashAwareErrorHandler>();

        services.AddSingleton<INavigationRegistry, NavigationRegistry>();
        services.AddTransient<NavigationState>();
        services.AddTransient<IInitializationTracker, InitializationTracker>();
        services.AddTransient<INavigationService, NavigationService>();
        services.AddSingleton<IShellContextFactory, ShellContextFactory>();

        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IWindowManager, WindowManager>();
        services.AddSingleton<IWindowRegistry, WindowRegistry>();
        services.AddSingleton<IUiDispatcher>(_ =>
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher
                             ?? System.Windows.Threading.Dispatcher.CurrentDispatcher;
            return new WpfUiDispatcher(dispatcher);
        });

        services.AddDbContextFactory<DataContext>(lifetime: ServiceLifetime.Singleton);
        services.AddTransient<DataContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<DataContext>>().CreateDbContext());

        services.AddSingleton<DbConnection>();
        services.AddSingleton<IDbConnection>(_ =>
        {
            var conn = new SqliteConnection(DbInfo.SqliteConnectionString);
            conn.Open();
            return conn;
        });

        services.AddSingleton<IGlobalHook, SharpHookHandler>();
        services.AddTransient<TimedTask>();
        services.AddSingleton<IBackgroundTaskStatusService, BackgroundTaskStatusService>();
    }

    public void RegisterNavigation(INavigationRegistry registry)
    {
    }
}
