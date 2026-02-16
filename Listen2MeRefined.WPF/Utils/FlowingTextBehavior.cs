namespace Listen2MeRefined.WPF.Utils;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Listen2MeRefined.WPF;

public static class FlowingTextBehavior
{
    private static readonly KeySpline EaseOutKeySpline = new(0.5, 0, 0.5, 1);
    private static readonly Dictionary<StoryboardTextBlock, Storyboard> Storyboards = new();

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(FlowingTextBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not StoryboardTextBlock textBlock)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            textBlock.MouseEnter += OnMouseEnter;
            textBlock.MouseLeave += OnMouseLeave;
            return;
        }

        textBlock.MouseEnter -= OnMouseEnter;
        textBlock.MouseLeave -= OnMouseLeave;
    }

    private static void OnMouseEnter(object sender, MouseEventArgs e)
    {
        var flowingTextBlock = (StoryboardTextBlock)sender;
        if (Storyboards.ContainsKey(flowingTextBlock))
        {
            return;
        }

        if (flowingTextBlock.Parent is not FrameworkElement container)
        {
            return;
        }

        var swipeAmount = flowingTextBlock.ActualWidth - container.ActualWidth;
        if (swipeAmount <= 0)
        {
            return;
        }

        var swipeSeconds = Math.Max(3, swipeAmount / 100 * 3.5d);

        var thicknessAnimation = new ThicknessAnimationUsingKeyFrames
        {
            RepeatBehavior = new RepeatBehavior(1),
            AutoReverse = true
        };

        thicknessAnimation.KeyFrames.Add(new SplineThicknessKeyFrame(
            new Thickness(0, 0, 0, 10),
            KeyTime.FromTimeSpan(TimeSpan.FromSeconds(.5))));

        thicknessAnimation.KeyFrames.Add(new SplineThicknessKeyFrame(
            new Thickness(-swipeAmount, 0, 0, 10),
            KeyTime.FromTimeSpan(TimeSpan.FromSeconds(swipeSeconds)),
            EaseOutKeySpline));

        thicknessAnimation.KeyFrames.Add(new SplineThicknessKeyFrame(
            new Thickness(-swipeAmount, 0, 0, 10),
            KeyTime.FromTimeSpan(TimeSpan.FromSeconds(swipeSeconds + .5)),
            EaseOutKeySpline));

        Storyboard.SetTarget(thicknessAnimation, flowingTextBlock);
        Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath("(TextBlock.Margin)"));

        var storyboard = new Storyboard();
        storyboard.Children.Add(thicknessAnimation);
        storyboard.Begin();
        Storyboards[flowingTextBlock] = storyboard;
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e)
    {
        var flowingTextBlock = (StoryboardTextBlock)sender;

        if (!Storyboards.TryGetValue(flowingTextBlock, out var runningStoryboard))
        {
            return;
        }

        var swipeAmount = flowingTextBlock.Margin.Left;
        var swipeSeconds = Math.Max(1, Math.Abs(swipeAmount / 100 * 1.33d));

        var thicknessAnimation = new ThicknessAnimationUsingKeyFrames
        {
            RepeatBehavior = new RepeatBehavior(1)
        };

        if (Math.Abs(swipeAmount) > 0)
        {
            thicknessAnimation.KeyFrames.Add(new SplineThicknessKeyFrame(
                new Thickness(swipeAmount, 0, 0, 10),
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                EaseOutKeySpline));

            thicknessAnimation.KeyFrames.Add(new SplineThicknessKeyFrame(
                new Thickness(0, 0, 0, 10),
                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(swipeSeconds)),
                EaseOutKeySpline));
        }

        Storyboard.SetTarget(thicknessAnimation, flowingTextBlock);
        Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath("(TextBlock.Margin)"));

        runningStoryboard.Stop();

        var storyboard = new Storyboard();
        storyboard.Children.Add(thicknessAnimation);
        storyboard.Completed += (_, _) => Storyboards.Remove(flowingTextBlock);
        storyboard.Begin();
    }
}
