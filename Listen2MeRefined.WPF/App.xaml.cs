using Listen2MeRefined.Application.Notifications;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Dapper;
using Listen2MeRefined.WPF.Dependency;
using Listen2MeRefined.WPF.Utils;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            
            WindowManager.Initialize(_host.Services);
            
            var window = _host.Services.GetRequiredService<MainWindow>();
            window.Show();

            _singleInstanceFileOpenBridge = new SingleInstanceFileOpenBridge(Log.Logger, _host.Services.GetRequiredService<IMediator>());
            if (!_singleInstanceFileOpenBridge.IsPrimaryInstance)
            {
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
                return;
            }

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

    private static IHostBuilder CreateHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();
        
        builder
            .ConfigureDataAccess()
            .ConfigureLogger()
            .ConfigureModules()
            .ConfigureMusicPlayer()
            .ConfigureNavigation()
            .ConfigureStartup()
            .ConfigureSystem()
            .ConfigureUtils()
            .ConfigureViewModels()
            .ConfigureViews()
            .ConfigureWaveForm()
            .ConfigureWrappers();
        
        return builder;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceFileOpenBridge?.Dispose();
        base.OnExit(e);
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
