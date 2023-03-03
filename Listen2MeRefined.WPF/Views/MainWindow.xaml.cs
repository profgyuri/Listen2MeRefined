using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Listen2MeRefined.WPF.Views;
using Listen2MeRefined.WPF.Views.Pages;

namespace Listen2MeRefined.WPF;

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

    #region Flowing text animation
    private void DisplayText_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var flowingTextBlock = (sender as StoryboardTextBlock)!;

        //if the storyboard is already playing, just return
        if (_storyboards.ContainsKey(flowingTextBlock.StoryboardName))
        {
            return;
        }

        //we retrieve the full width of the text which is bound to the tag property
        var fullWidth = (double)flowingTextBlock.Tag;
        double swipeAmount = fullWidth - flowingTextBlock.ActualWidth;

        //getting a smooth amount of swipe duration for longer texts
        var swipeSeconds = swipeAmount / 100 * 2d;

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
                new Thickness(0, 0, 0, 0),
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(.5))));

        if (swipeAmount > 0)
        {
            thicknessAnimation.KeyFrames.Add(
                new SplineThicknessKeyFrame(
                    new Thickness(-swipeAmount, 0, 0, 0),
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(swipeSeconds)),
                    _easeOutKeySpline));

            thicknessAnimation.KeyFrames.Add(
                new SplineThicknessKeyFrame(
                    new Thickness(-swipeAmount, 0, 0, 0),
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
                    new Thickness(swipeAmount, 0, 0, 0),
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                    _easeOutKeySpline));

            thicknessAnimation.KeyFrames.Add(
               new SplineThicknessKeyFrame(
                   new Thickness(0, 0, 0, 0),
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
    }
}