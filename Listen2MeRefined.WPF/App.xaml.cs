namespace Listen2MeRefined.WPF;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Autofac;
using Dapper;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.WPF.Dependency;
using Listen2MeRefined.WPF.Utils;
using Serilog;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public sealed partial class App : Application
{
    private SingleInstanceFileOpenBridge? _singleInstanceFileOpenBridge;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Subscribe as early as possible
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        base.OnStartup(e);

        SqlMapper.AddTypeHandler(new TimeSpanTypeHandler());
        RenderOptions.ProcessRenderMode = RenderMode.Default;

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        try
        {
            using var startupScope = IocContainer.GetContainer().BeginLifetimeScope();
            var logger = startupScope.Resolve<ILogger>();
            var mediator = startupScope.Resolve<MediatR.IMediator>();

            _singleInstanceFileOpenBridge = new SingleInstanceFileOpenBridge(logger);
            if (!_singleInstanceFileOpenBridge.IsPrimaryInstance)
            {
                using var forwardTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                var forwarded = _singleInstanceFileOpenBridge
                    .ForwardToPrimaryAsync(e.Args, forwardTimeout.Token)
                    .GetAwaiter()
                    .GetResult();

                if (!forwarded)
                {
                    logger.Warning("[App] Failed to forward shell-open args to primary instance");
                }

                Shutdown(0);
                return;
            }

            WindowManager.ShowMainWindow<MainWindow>();

            if (e.Args.Length > 0)
            {
                _ = mediator.Publish(new ExternalAudioFilesOpenedNotification(e.Args));
            }
        }
        catch (Exception ex)
        {
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceFileOpenBridge?.Dispose();
        base.OnExit(e);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        using var scope = IocContainer.GetContainer().BeginLifetimeScope();

        Exception ex = e.ExceptionObject as Exception;
        string errorMessage = ex is not null ? "Unahandled exception: " + ex.Message + "\n" + ex.StackTrace : "Unknown error occurred.";

        var logger = scope.Resolve<ILogger>();
        logger.Fatal(errorMessage);

        MessageBox.Show("The application has crashed! If you wish to help to resolve the issue, please, send the latest " +
            "log.txt file (in the same folder as listen2me.exe) to 'listen2mebugs@gmail.com'!", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
