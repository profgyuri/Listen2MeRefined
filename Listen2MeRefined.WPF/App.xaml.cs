﻿namespace Listen2MeRefined.WPF;

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
        WindowManager.ShowWindow<MainWindow>(false);
    }
}
