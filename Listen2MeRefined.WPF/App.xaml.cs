using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Dependency;
using Listen2MeRefined.WPF.ErrorHandling;
using Listen2MeRefined.WPF.Utils.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using AppLoggerConfiguration = Listen2MeRefined.WPF.Dependency.LoggerConfiguration;

namespace Listen2MeRefined.WPF;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public sealed partial class App : System.Windows.Application
{
    private IErrorHandler _errorHandler;
    private IHost? _host;
    private SingleInstanceFileOpenBridge? _singleInstanceFileOpenBridge;

    public App()
    {
        _errorHandler = CreateFallbackErrorHandler();

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
            _singleInstanceFileOpenBridge = new SingleInstanceFileOpenBridge(Log.Logger);
            if (ProcessFileOpenForwarding(e))
            {
                return;
            }

            _host = CreateHostBuilder().Build();
            _host.ConfigureModuleNavigation();

            _errorHandler = _host.Services.GetRequiredService<IErrorHandler>();

            var messenger = _host.Services.GetRequiredService<IMessenger>();
            _singleInstanceFileOpenBridge.AttachMessenger(messenger);
            await _host.StartAsync().ConfigureAwait(true);

            var windowManager = _host.Services.GetRequiredService<IWindowManager>();
            await windowManager.ShowMainWindowAsync<MainShellViewModel>();

            if (e.Args.Length > 0)
            {
                messenger.Send(new ExternalAudioFilesOpenedMessage(e.Args));
            }
        }
        catch (Exception ex)
        {
            ReportUnhandled(
                ex,
                UnhandledErrorSource.Startup,
                isTerminating: true,
                context: nameof(OnStartup));
            ShutdownWithCode(-1);
        }
    }

    /// <summary>
    /// Forward file open args to the primary instance.
    /// </summary>
    /// <param name="e">Startup event arguments.</param>
    /// <returns><see langword="False"/> if the current instance is the primary;
    /// otherwise <see langword="True"/>, if the args were forwarded.</returns>
    private bool ProcessFileOpenForwarding(StartupEventArgs e)
    {
        if (_singleInstanceFileOpenBridge is null)
        {
            throw new InvalidOperationException("Single-instance bridge has not been initialized.");
        }

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
        return Host.CreateDefaultBuilder()
            .ConfigureModuleServices();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _singleInstanceFileOpenBridge?.Dispose();

        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        await Log.CloseAndFlushAsync();

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportUnhandled(
            e.Exception,
            UnhandledErrorSource.Dispatcher,
            isTerminating: true,
            context: nameof(OnDispatcherUnhandledException));

        e.Handled = true;
        ShutdownWithCode(-1);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            ReportUnhandled(
                exception,
                UnhandledErrorSource.AppDomain,
                isTerminating: e.IsTerminating,
                context: nameof(OnUnhandledException));
        }
        else
        {
            var fallback = new InvalidOperationException(
                $"AppDomain unhandled exception object was not an Exception: {e.ExceptionObject}");
            ReportUnhandled(
                fallback,
                UnhandledErrorSource.AppDomain,
                isTerminating: e.IsTerminating,
                context: nameof(OnUnhandledException));
        }

        ShutdownWithCode(-1);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ReportUnhandled(
            e.Exception,
            UnhandledErrorSource.TaskScheduler,
            isTerminating: true,
            context: nameof(OnUnobservedTaskException));

        e.SetObserved();
        ShutdownWithCode(-1);
    }

    private void ReportUnhandled(
        Exception exception,
        UnhandledErrorSource source,
        bool isTerminating,
        string context)
    {
        var errorContext = new UnhandledErrorContext(
            source,
            isTerminating,
            DateTimeOffset.UtcNow,
            context);

        try
        {
            _errorHandler
                .HandleUnhandledAsync(exception, errorContext)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception reportException)
        {
            Log.Logger.Fatal(
                reportException,
                "Failed while reporting unhandled exception. Source={Source} Context={Context}",
                source,
                context);
        }
    }

    private void ShutdownWithCode(int exitCode)
    {
        try
        {
            if (Dispatcher.CheckAccess())
            {
                Shutdown(exitCode);
                return;
            }

            Dispatcher.Invoke(() => Shutdown(exitCode));
        }
        catch
        {
            Environment.Exit(exitCode);
        }
    }

    private IErrorHandler CreateFallbackErrorHandler()
    {
        var logLocationService = new LocalAppDataLogLocationService();
        logLocationService.EnsureLogDirectoryExists();

        var logger = AppLoggerConfiguration.CreateLogger(logLocationService);
        Log.Logger = logger;

        var uiDispatcher = new WpfUiDispatcher(Dispatcher);
        var crashDialogService = new CrashDialogService(uiDispatcher, logLocationService, logger);
        return new CrashAwareErrorHandler(logger, crashDialogService, logLocationService);
    }
}
