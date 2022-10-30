using System.Windows;
using Dapper;

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
    }
}