using System.Windows.Controls;

namespace Listen2MeRefined.WPF.Utils;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Listen2MeRefined.WPF;

public static class FlowingTextBehavior
{
    private static readonly KeySpline EaseOutKeySpline = new(0.5, 0, 0.5, 1);
    private static readonly Dictionary<StoryboardTextBlock, Storyboard> Storyboards = new();
    private static readonly Dictionary<StoryboardTextBlock, DispatcherTimer> AutoFlowTimers = new();

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(FlowingTextBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static readonly DependencyProperty IsAutoFlowProperty = DependencyProperty.RegisterAttached(
        "IsAutoFlow",
        typeof(bool),
        typeof(FlowingTextBehavior),
        new PropertyMetadata(false, OnIsAutoFlowChanged));

    public static bool GetIsAutoFlow(DependencyObject obj) => (bool)obj.GetValue(IsAutoFlowProperty);
    public static void SetIsAutoFlow(DependencyObject obj, bool value) => obj.SetValue(IsAutoFlowProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not StoryboardTextBlock textBlock)
        {
            return;
        }

        var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));

        if ((bool)e.NewValue)
        {
            if (GetIsAutoFlow(textBlock))
            {
                descriptor.AddValueChanged(textBlock, OnTextChangedForAutoFlow);
            }
            else
            {
                textBlock.MouseEnter += OnMouseEnter;
                textBlock.MouseLeave += OnMouseLeave;
            }
            return;
        }

        textBlock.MouseEnter -= OnMouseEnter;
        textBlock.MouseLeave -= OnMouseLeave;
        descriptor.RemoveValueChanged(textBlock, OnTextChangedForAutoFlow);
        CancelAutoFlowTimer(textBlock);
    }

    private static void OnIsAutoFlowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not StoryboardTextBlock textBlock || !GetIsEnabled(textBlock))
        {
            return;
        }

        var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));

        if ((bool)e.NewValue)
        {
            textBlock.MouseEnter -= OnMouseEnter;
            textBlock.MouseLeave -= OnMouseLeave;
            descriptor.AddValueChanged(textBlock, OnTextChangedForAutoFlow);
        }
        else
        {
            CancelAutoFlowTimer(textBlock);
            descriptor.RemoveValueChanged(textBlock, OnTextChangedForAutoFlow);
            textBlock.MouseEnter += OnMouseEnter;
            textBlock.MouseLeave += OnMouseLeave;
        }
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

    private static void OnTextChangedForAutoFlow(object? sender, EventArgs e)
    {
        if (sender is not StoryboardTextBlock textBlock)
        {
            return;
        }

        StopRunningStoryboard(textBlock);
        CancelAutoFlowTimer(textBlock);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            AutoFlowTimers.Remove(textBlock);
            TriggerAutoScroll(textBlock);
        };
        timer.Start();
        AutoFlowTimers[textBlock] = timer;
    }

    private static void TriggerAutoScroll(StoryboardTextBlock textBlock)
    {
        if (textBlock.Parent is not FrameworkElement container)
        {
            return;
        }

        var swipeAmount = textBlock.ActualWidth - container.ActualWidth;
        if (swipeAmount <= 0)
        {
            return;
        }

        StopRunningStoryboard(textBlock);

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

        Storyboard.SetTarget(thicknessAnimation, textBlock);
        Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath("(TextBlock.Margin)"));

        var storyboard = new Storyboard();
        storyboard.Children.Add(thicknessAnimation);
        storyboard.Completed += (_, _) => Storyboards.Remove(textBlock);
        storyboard.Begin();
        Storyboards[textBlock] = storyboard;
    }

    private static void StopRunningStoryboard(StoryboardTextBlock textBlock)
    {
        if (!Storyboards.TryGetValue(textBlock, out var running))
        {
            return;
        }

        running.Stop();
        Storyboards.Remove(textBlock);
    }

    private static void CancelAutoFlowTimer(StoryboardTextBlock textBlock)
    {
        if (!AutoFlowTimers.TryGetValue(textBlock, out var timer))
        {
            return;
        }

        timer.Stop();
        AutoFlowTimers.Remove(textBlock);
    }
}
