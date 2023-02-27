using System;
using System.Windows;
using Listen2MeRefined.WPF.Views;
using Listen2MeRefined.WPF.Views.Pages;

namespace Listen2MeRefined.WPF;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel viewModel,
        CurrentlyPlayingPage currentlyPlayingPage)
    {
        InitializeComponent();

        DataContext = viewModel;
        CurrentlyPlayingFrame.Content = currentlyPlayingPage;
    }

    private void CloseWindow_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
        Environment.Exit(0);
    }

    private void MaximizeWindow_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState =
            WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }

    private void MinimizeWindow_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void SettingsWindow_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowManager.ShowWindow<SettingsWindow>(Left + Width / 2, Top + Height / 2);
    }

    private void AdvancedSearchWindow_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowManager.ShowWindow<AdvancedSearchWindow>(Left + Width / 2, Top + Height / 2);
    }

    private void WindowsFormsHost_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        var vm = (MainWindowViewModel)DataContext;
        vm.RefreshSoundWave().ConfigureAwait(false);
    }
}