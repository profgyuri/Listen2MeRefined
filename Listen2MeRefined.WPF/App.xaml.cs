using Listen2MeRefined.Application.Notifications;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Dapper;
using Listen2MeRefined.Application.Modules;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Dependency;
using Listen2MeRefined.WPF.Utils;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Listen2MeRefined.WPF;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public sealed partial class App : System.Windows.Application
{
    private IHost? _host;
    private SingleInstanceFileOpenBridge? _singleInstanceFileOpenBridge;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        SqlMapper.AddTypeHandler(new TimeSpanTypeHandler());
        RenderOptions.ProcessRenderMode = RenderMode.Default;

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        try
        {
            _host = CreateHostBuilder().Build();
            await _host.StartAsync().ConfigureAwait(true);
            
            if (ProcessFileOpenForwarding(e)) return;
            
            RegisterNavigation(_host.Services);
            
            var window = _host.Services.GetRequiredService<MainShellView>();
            window.Show();

            await _host.Services.GetRequiredService<MainShellViewModel>().EnsureInitializedAsync();

            if (e.Args.Length > 0)
            {
                var mediator = _host.Services.GetRequiredService<IMediator>();
                await mediator.Publish(new ExternalAudioFilesOpenedNotification(e.Args));
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed.");
            Shutdown(-1);
        }
    }

    /// <summary>
    /// Forward file open args to the primary instance.
    /// </summary>
    /// <param name="e">Startup event arguments.</param>
    /// <returns><see langword="False"/> if the current instance is the primary;
    /// otherwise <see langword="True"/>, if the args were forwarded.</returns>"/>
    private bool ProcessFileOpenForwarding(StartupEventArgs e)
    {
        _singleInstanceFileOpenBridge = 
            new SingleInstanceFileOpenBridge(Log.Logger, _host!.Services.GetRequiredService<IMediator>());
        if (_singleInstanceFileOpenBridge.IsPrimaryInstance) return false;
        
        using var forwardTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        var forwarded = _singleInstanceFileOpenBridge
            .ForwardToPrimaryAsync(e.Args, forwardTimeout.Token)
            .GetAwaiter()
            .GetResult();

        if (!forwarded)
        {
            Log.Logger.Warning("[App] Failed to forward shell-open args to primary instance");
        }

        Shutdown(0);
        return true;

    }

    private static IHostBuilder CreateHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();
        
        builder
            .ConfigureDataAccess()
            .ConfigureLogger()
            .ConfigureNavigation()
            .ConfigureModules()
            .ConfigureMusicPlayer()
            .ConfigureStartup()
            .ConfigureSystem()
            .ConfigureUtils()
            .ConfigureWaveForm()
            .ConfigureWrappers();
        
        return builder;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _singleInstanceFileOpenBridge?.Dispose();
        
        if (_host is not null)
        {
            try
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            finally
            {
                _host.Dispose();
            }
        }
        
        await Log.CloseAndFlushAsync();
        
        base.OnExit(e);
    }
    
    private static void RegisterNavigation(IServiceProvider services)
    {
        var moduleCatalog = services.GetRequiredService<IModuleCatalog>();
        var navigationRegistry = services.GetRequiredService<INavigationRegistry>();

        foreach (var module in moduleCatalog.LoadModules())
        {
            module.RegisterNavigation(navigationRegistry);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled dispatcher exception.");
        e.Handled = true;
    }
    
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Log.Fatal(exception, "Unhandled AppDomain exception.");
        }
    }
    
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception.");
        e.SetObserved();
    }
}
