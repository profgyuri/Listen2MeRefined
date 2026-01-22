namespace Listen2MeRefined.WPF;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Autofac;
using Dapper;
using Listen2MeRefined.WPF.Dependency;
using Serilog;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public sealed partial class App : Application
{
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
            WindowManager.ShowMainWindow<MainWindow>();
        }
        catch (Exception ex)
        {
            // log / show message, then shut down cleanly
            MessageBox.Show(ex.ToString(), "Startup failed");
            Shutdown(-1);
        }
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