namespace Listen2MeRefined.WPF;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

[TemplatePart(Name = "PART_AniVerticalScrollBar", Type = typeof(ScrollBar))]
[TemplatePart(Name = "PART_AniHorizontalScrollBar", Type = typeof(ScrollBar))]
public class SmoothScrollViewer : ScrollViewer
{
    private ScrollBar _aniHorizontalScrollBar;
    private ScrollBar _aniVerticalScrollBar;

    public static readonly DependencyProperty _targetVerticalOffsetProperty =
        DependencyProperty.Register(nameof(TargetVerticalOffset), typeof(double), typeof(SmoothScrollViewer),
            new PropertyMetadata(0.0, OnTargetVerticalOffsetChanged));

    public static readonly DependencyProperty _targetHorizontalOffsetProperty =
        DependencyProperty.Register(nameof(TargetHorizontalOffset), typeof(double), typeof(SmoothScrollViewer),
            new PropertyMetadata(0.0, OnTargetHorizontalOffsetChanged));

    public static readonly DependencyProperty _horizontalScrollOffsetProperty =
        DependencyProperty.Register("HorizontalScrollOffset", typeof(double), typeof(SmoothScrollViewer),
            new PropertyMetadata(0.0, OnHorizontalScrollOffsetChanged));

    public static readonly DependencyProperty _verticalScrollOffsetProperty =
        DependencyProperty.Register("VerticalScrollOffset", typeof(double), typeof(SmoothScrollViewer),
            new PropertyMetadata(0.0, OnVerticalScrollOffsetChanged));

    public static readonly DependencyProperty _scrollingTimeProperty =
        DependencyProperty.Register("ScrollingTime", typeof(TimeSpan), typeof(SmoothScrollViewer),
            new PropertyMetadata(new TimeSpan(0, 0, 0,
                0, 500)));

    public static readonly DependencyProperty _scrollingSplineProperty =
        DependencyProperty.Register("ScrollingSpline", typeof(KeySpline), typeof(SmoothScrollViewer),
            new PropertyMetadata(new KeySpline(0.024, 0.914, 0.717,
                1)));

    public static readonly DependencyProperty _canKeyboardScrollProperty =
        DependencyProperty.Register("CanKeyboardScroll", typeof(bool), typeof(SmoothScrollViewer),
            new FrameworkPropertyMetadata(true));

    /// <summary>
    ///     This is the VerticalOffset that we'd like to animate to.
    /// </summary>
    private double TargetVerticalOffset
    {
        get => (double) GetValue(_targetVerticalOffsetProperty);
        set => SetValue(_targetVerticalOffsetProperty, value);
    }

    /// <summary>
    ///     This is the HorizontalOffset that we'll be animating to.
    /// </summary>
    private double TargetHorizontalOffset
    {
        get => (double) GetValue(_targetHorizontalOffsetProperty);
        set => SetValue(_targetHorizontalOffsetProperty, value);
    }

    /// <summary>
    ///     A property for changing the time it takes to scroll to a new position.
    /// </summary>
    private TimeSpan ScrollingTime => (TimeSpan) GetValue(_scrollingTimeProperty);

    /// <summary>
    ///     A property to allow users to describe a custom spline for the scrolling animation.
    /// </summary>
    private KeySpline ScrollingSpline => (KeySpline) GetValue(_scrollingSplineProperty);

    private bool CanKeyboardScroll => (bool) GetValue(_canKeyboardScrollProperty);

    static SmoothScrollViewer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SmoothScrollViewer),
            new FrameworkPropertyMetadata(typeof(SmoothScrollViewer)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_AniVerticalScrollBar") is ScrollBar aniVScroll)
        {
            _aniVerticalScrollBar = aniVScroll;
        }

        _aniVerticalScrollBar.ValueChanged += VScrollBar_ValueChanged;

        if (GetTemplateChild("PART_AniHorizontalScrollBar") is ScrollBar aniHScroll)
        {
            _aniHorizontalScrollBar = aniHScroll;
        }

        _aniHorizontalScrollBar.ValueChanged += HScrollBar_ValueChanged;

        PreviewMouseWheel += CustomPreviewMouseWheel;
        PreviewKeyDown += AnimatedScrollViewer_PreviewKeyDown;
    }

    private static void AnimatedScrollViewer_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        var thisScroller = (SmoothScrollViewer) sender;

        if (!thisScroller.CanKeyboardScroll)
        {
            return;
        }

        var keyPressed = e.Key;
        var newVerticalPos = thisScroller.TargetVerticalOffset;
        var newHorizontalPos = thisScroller.TargetHorizontalOffset;
        bool isKeyHandled;

        switch (keyPressed)
        {
            //Vertical Key Strokes code
            case Key.Down:
                newVerticalPos = NormalizeScrollPos(thisScroller, newVerticalPos + 16.0, Orientation.Vertical);
                isKeyHandled = true;
                break;
            case Key.PageDown:
                newVerticalPos = NormalizeScrollPos(thisScroller, newVerticalPos + thisScroller.ViewportHeight,
                    Orientation.Vertical);
                isKeyHandled = true;
                break;
            case Key.Up:
                newVerticalPos = NormalizeScrollPos(thisScroller, newVerticalPos - 16.0, Orientation.Vertical);
                isKeyHandled = true;
                break;
            case Key.PageUp:
                newVerticalPos = NormalizeScrollPos(thisScroller, newVerticalPos - thisScroller.ViewportHeight,
                    Orientation.Vertical);
                isKeyHandled = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e), "Unhandled navigation key pressed!");
        }

        if (Math.Abs(newVerticalPos - thisScroller.TargetVerticalOffset) > 0.0F)
        {
            thisScroller.TargetVerticalOffset = newVerticalPos;
        }

        switch (keyPressed)
        {
            //Horizontal Key Strokes Code
            case Key.Right:
                newHorizontalPos = NormalizeScrollPos(thisScroller, newHorizontalPos + 16, Orientation.Horizontal);
                isKeyHandled = true;
                break;
            case Key.Left:
                newHorizontalPos = NormalizeScrollPos(thisScroller, newHorizontalPos - 16, Orientation.Horizontal);
                isKeyHandled = true;
                break;
        }

        if (Math.Abs(newHorizontalPos - thisScroller.TargetHorizontalOffset) > 0.0F)
        {
            thisScroller.TargetHorizontalOffset = newHorizontalPos;
        }

        e.Handled = isKeyHandled;
    }

    private static double NormalizeScrollPos(
        ScrollViewer thisScroll,
        double scrollChange,
        Orientation o)
    {
        var returnValue = scrollChange;

        if (scrollChange < 0)
        {
            returnValue = 0;
        }

        if (o == Orientation.Vertical && scrollChange > thisScroll.ScrollableHeight)
        {
            return thisScroll.ScrollableHeight;
        }

        if (o == Orientation.Horizontal && scrollChange > thisScroll.ScrollableWidth)
        {
            return thisScroll.ScrollableWidth;
        }

        return returnValue;
    }

    private static void CustomPreviewMouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        double mouseWheelChange = e.Delta;

        var thisScroller = (SmoothScrollViewer) sender;
        var newVOffset = thisScroller.TargetVerticalOffset - mouseWheelChange / 3;

        if (newVOffset < 0)
        {
            thisScroller.TargetVerticalOffset = 0;
        }
        else if (newVOffset > thisScroller.ScrollableHeight)
        {
            thisScroller.TargetVerticalOffset = thisScroller.ScrollableHeight;
        }
        else
        {
            thisScroller.TargetVerticalOffset = newVOffset;
        }

        e.Handled = true;
    }

    private void VScrollBar_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        var thisScroller = this;
        var oldTargetVOffset = e.OldValue;
        var newTargetVOffset = e.NewValue;

        if (Math.Abs(newTargetVOffset - thisScroller.TargetVerticalOffset) <= 0.0F)
        {
            return;
        }

        var deltaVOffset = Math.Round(newTargetVOffset - oldTargetVOffset, 3);

        thisScroller.TargetVerticalOffset =
            deltaVOffset switch
            {
                1 => oldTargetVOffset + thisScroller.ViewportWidth,
                -1 => oldTargetVOffset - thisScroller.ViewportWidth,
                0.1 => oldTargetVOffset + 16.0,
                -0.1 => oldTargetVOffset - 16.0,
                _ => newTargetVOffset
            };
    }

    private void HScrollBar_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        var thisScroller = this;

        var oldTargetHOffset = e.OldValue;
        var newTargetHOffset = e.NewValue;

        if (Math.Abs(newTargetHOffset - thisScroller.TargetHorizontalOffset) <= 0.0F)
        {
            return;
        }

        var deltaVOffset = Math.Round(newTargetHOffset - oldTargetHOffset, 3);

        thisScroller.TargetHorizontalOffset =
            deltaVOffset switch
            {
                1 => oldTargetHOffset + thisScroller.ViewportWidth,
                -1 => oldTargetHOffset - thisScroller.ViewportWidth,
                0.1 => oldTargetHOffset + 16.0,
                -0.1 => oldTargetHOffset - 16.0,
                _ => newTargetHOffset
            };
    }

    private static void OnTargetVerticalOffsetChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var thisScroller = (SmoothScrollViewer) d;

        if (Math.Abs((double) e.NewValue - thisScroller._aniVerticalScrollBar.Value) > 0.0D)
        {
            thisScroller._aniVerticalScrollBar.Value = (double) e.NewValue;
        }

        AnimateScroller(thisScroller);
    }

    private static void OnTargetHorizontalOffsetChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var thisScroller = (SmoothScrollViewer) d;

        if (Math.Abs((double) e.NewValue - thisScroller._aniHorizontalScrollBar.Value) > 0.0D)
        {
            thisScroller._aniHorizontalScrollBar.Value = (double) e.NewValue;
        }

        AnimateScroller(thisScroller);
    }

    private static void OnHorizontalScrollOffsetChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var thisSViewer = (SmoothScrollViewer) d;
        thisSViewer.ScrollToHorizontalOffset((double) e.NewValue);
    }

    private static void OnVerticalScrollOffsetChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var thisSViewer = (SmoothScrollViewer) d;
        thisSViewer.ScrollToVerticalOffset((double) e.NewValue);
    }

    private static void AnimateScroller(object objectToScroll)
    {
        var thisScrollViewer = objectToScroll as SmoothScrollViewer;

        KeyTime targetKeyTime = thisScrollViewer!.ScrollingTime;
        var targetKeySpline = thisScrollViewer.ScrollingSpline;

        DoubleAnimationUsingKeyFrames animateHScrollKeyFramed = new();
        DoubleAnimationUsingKeyFrames animateVScrollKeyFramed = new();

        SplineDoubleKeyFrame hScrollKey1 = new(thisScrollViewer.TargetHorizontalOffset, targetKeyTime, targetKeySpline);
        SplineDoubleKeyFrame vScrollKey1 = new(thisScrollViewer.TargetVerticalOffset, targetKeyTime, targetKeySpline);
        animateHScrollKeyFramed.KeyFrames.Add(hScrollKey1);
        animateVScrollKeyFramed.KeyFrames.Add(vScrollKey1);

        thisScrollViewer.BeginAnimation(_horizontalScrollOffsetProperty, animateHScrollKeyFramed);
        thisScrollViewer.BeginAnimation(_verticalScrollOffsetProperty, animateVScrollKeyFramed);
    }
}