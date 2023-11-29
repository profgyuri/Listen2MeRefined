namespace Listen2MeRefined.WPF;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.WPF.Views;
using Listen2MeRefined.WPF.Views.Pages;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly KeySpline _easeOutKeySpline = new(0.5, 0, 0.5, 1);
    private readonly Dictionary<string, Storyboard> _storyboards = new();

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
        Application.Current.Shutdown();
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
        UpdateNotification.Visibility = Visibility.Collapsed;
        WindowManager.ShowWindow<SettingsWindow>(Left + Width / 2, Top + Height / 2);
    }

    private void AdvancedSearchWindow_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowManager.ShowWindow<AdvancedSearchWindow>(Left + Width / 2, Top + Height / 2);
        var vm = (MainWindowViewModel)DataContext;
        vm.ListsViewModel.SwitchToSearchResultsTab();
    }

    #region Flowing text animation
    private void DisplayText_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var flowingTextBlock = (sender as StoryboardTextBlock)!;
        var container = flowingTextBlock.Parent as FrameworkElement;

        //if the storyboard is already playing, just return
        if (_storyboards.ContainsKey(flowingTextBlock.StoryboardName))
        {
            return;
        }

        double swipeAmount = flowingTextBlock.ActualWidth - container.ActualWidth + 0;

        //getting a smooth amount of swipe duration for longer texts
        var swipeSeconds = swipeAmount / 100 * 3.5d;

        //setting a minimum animation duration, so short texts dont just "jump" around
        swipeSeconds = swipeSeconds >= 3 ? swipeSeconds : 3;

        if (swipeAmount <= 0)
        {
            return;
        }

        var thicknessAnimation = new ThicknessAnimationUsingKeyFrames()
        {
            RepeatBehavior = new RepeatBehavior(1),
            AutoReverse = true
        };
        thicknessAnimation.KeyFrames.Add(
            new SplineThicknessKeyFrame(
                new Thickness(0, 0, 0, 10),
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(.5))));

        if (swipeAmount > 0)
        {
            thicknessAnimation.KeyFrames.Add(
                new SplineThicknessKeyFrame(
                    new Thickness(-swipeAmount, 0, 0, 10),
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(swipeSeconds)),
                    _easeOutKeySpline));

            thicknessAnimation.KeyFrames.Add(
                new SplineThicknessKeyFrame(
                    new Thickness(-swipeAmount, 0, 0, 10),
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(swipeSeconds + .5)),
                    _easeOutKeySpline));
        }

        Storyboard.SetTarget(thicknessAnimation, flowingTextBlock);
        Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath("(TextBlock.Margin)"));

        var storyboard = new Storyboard();
        var name = $"a{Guid.NewGuid():N}";
        storyboard.Name = name;
        flowingTextBlock.StoryboardName = name;
        storyboard.Children.Add(thicknessAnimation);
        storyboard.Begin();
        _storyboards.Add(storyboard.Name, storyboard);
    }

    private void DisplayText_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var FlowingTetxBlock = (sender as StoryboardTextBlock)!;

        if (!_storyboards.ContainsKey(FlowingTetxBlock.StoryboardName))
        {
            return;
        }

        double swipeAmount = FlowingTetxBlock.Margin.Left;

        //getting a smooth amount of swipe duration for longer texts
        var swipeSeconds = Math.Abs(swipeAmount / 100 * 1.33d);
        swipeSeconds = swipeSeconds >= 1 ? swipeSeconds : 1;

        var thicknessAnimation = new ThicknessAnimationUsingKeyFrames()
        {
            RepeatBehavior = new RepeatBehavior(1)
        };

        if (Math.Abs(swipeAmount) > 0)
        {
            thicknessAnimation.KeyFrames.Add(
                new SplineThicknessKeyFrame(
                    new Thickness(swipeAmount, 0, 0, 10),
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                    _easeOutKeySpline));

            thicknessAnimation.KeyFrames.Add(
               new SplineThicknessKeyFrame(
                   new Thickness(0, 0, 0, 10),
                   KeyTime.FromTimeSpan(TimeSpan.FromSeconds(swipeSeconds)),
                   _easeOutKeySpline));
        }

        Storyboard.SetTarget(thicknessAnimation, FlowingTetxBlock);
        Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath("(TextBlock.Margin)"));

        var name = FlowingTetxBlock.StoryboardName;
        var storyboard = _storyboards[name];
        storyboard.Stop();
        storyboard = new();
        storyboard.Name = name;
        storyboard.Children.Add(thicknessAnimation);
        storyboard.Completed += (s, e) => _storyboards.Remove(storyboard.Name);
        storyboard.Begin();
    }
    #endregion

    private void Playlist_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem != null)
        {
            listView.ScrollIntoView(listView.SelectedItem);
        }

        var vm = (MainWindowViewModel)DataContext;

        foreach (var audio in e.AddedItems)
        {
            vm.ListsViewModel.AddSelectedPlaylistItems((AudioModel)audio);
        }

        foreach (var audio in e.RemovedItems)
        {
            vm.ListsViewModel.RemoveSelectedPlaylistItems((AudioModel)audio);
        }
    }

    private void SearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = (MainWindowViewModel)DataContext;

        foreach (var audio in e.AddedItems)
        {
            vm.ListsViewModel.AddSelectedSearchResult((AudioModel)audio);
        }
        
        foreach (var audio in e.RemovedItems)
        {
            vm.ListsViewModel.RemoveSelectedSearchResult((AudioModel)audio);
        }
    }

    private void Playlist_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            var vm = (MainWindowViewModel)DataContext;
            vm.ListsViewModel.JumpToSelecteSong().ConfigureAwait(false);
        }
    }

    private void Playlist_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Right)
        {
            var vm = (MainWindowViewModel)DataContext;
            vm.ListsViewModel.SwitchToSongMenuTab();
        }
    }
}