using System;
using System.Windows;
using Autofac;
using Dapper;
using Listen2MeRefined.WPF.Dependency;
using Serilog;

namespace Listen2MeRefined.WPF;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public sealed partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SqlMapper.AddTypeHandler(new TimeSpanTypeHandler());
        WindowManager.ShowWindow<MainWindow>(false);

        // Subscribe to the UnhandledException event
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        using var scope = IocContainer.GetContainer().BeginLifetimeScope();

        Exception ex = e.ExceptionObject as Exception;
        string errorMessage = ex is not null ? "Unahandled exception: " + ex.Message + "\n" + ex.StackTrace : "Unknown error occurred.";

        var logger = scope.Resolve<ILogger>();
        logger.Fatal(errorMessage);
    }
}