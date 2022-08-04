namespace Listen2MeRefined.WPF;

using Autofac;
using System.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        var container = IocContainer.GetContainer();
        using var scope = container.BeginLifetimeScope();
        var main = scope.Resolve<MainWindow>();
        main.Show();
    }
}
