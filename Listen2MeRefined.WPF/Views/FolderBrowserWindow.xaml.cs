﻿namespace Listen2MeRefined.WPF;
using System.Windows;

/// <summary>
///     Interaction logic for FolderBrowserWindow.xaml
/// </summary>
public sealed partial class FolderBrowserWindow : Window
{
    public FolderBrowserWindow(FolderBrowserViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        FolderList.Focus();
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}