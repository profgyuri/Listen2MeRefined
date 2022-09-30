using Dapper;

namespace Listen2MeRefined.WPF;

using System.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public sealed partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SqlMapper.AddTypeHandler(new TimeSpanTypeHandler());
        ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        WindowManager.ShowWindow<MainWindow>(false);
    }
}
