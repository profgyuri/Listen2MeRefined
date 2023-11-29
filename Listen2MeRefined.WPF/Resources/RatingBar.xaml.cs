namespace Listen2MeRefined.WPF.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

/// <summary>
/// Interaction logic for RatingBar.xaml
/// </summary>
public partial class RatingBar : UserControl
{
    public RatingBar()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty RatingValueProperty = DependencyProperty.Register(
    "RatingValue", typeof(int), typeof(RatingBar), new PropertyMetadata(0, OnRatingValueChanged));

    public int RatingValue
    {
        get { return (int)GetValue(RatingValueProperty); }
        set { SetValue(RatingValueProperty, value); }
    }

    private static void OnRatingValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as RatingBar;
        int newRatingValue = (int)e.NewValue;
        // Update visual state of control here based on newRatingValue
    }

    private void OnRatingButtonClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        RatingValue = int.Parse(button.Tag.ToString());

        var parentGrid = button.Parent as Grid;

        // Loop through each child control in the Grid
        foreach (var child in parentGrid.Children)
        {
            // Check if the child control is a Button
            if (child is Button childButton && int.TryParse(childButton.Tag.ToString(), out int childRatingValue))
            {
                // Change the color of each button which has the same or lesser value of what's being hovered or clicked on
                if (childRatingValue <= RatingValue)
                {
                    childButton.Foreground = (SolidColorBrush)FindResource("ThirnaryBrush");
                }
                else
                {
                    childButton.Foreground = (SolidColorBrush)FindResource("ThirnaryMidBrush");
                }
            }
        }
    }

    private void Button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var button = sender as Button;
        var previewValue = int.Parse(button.Tag.ToString());

        var parentGrid = button.Parent as Grid;

        // Loop through each child control in the Grid
        foreach (var child in parentGrid.Children)
        {
            // Check if the child control is a Button
            if (child is Button childButton && int.TryParse(childButton.Tag.ToString(), out int childRatingValue))
            {
                // Change the color of each button which has the same or lesser value of what's being hovered or clicked on
                if (childRatingValue <= previewValue)
                {
                    childButton.Foreground = (SolidColorBrush)FindResource("ThirnaryLightBrush");
                }
                else
                {
                    childButton.Foreground = (SolidColorBrush)FindResource("ThirnaryMidBrush");
                }
            }
        }
    }

    private void Button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var button = sender as Button;

        var parentGrid = button.Parent as Grid;

        // Loop through each child control in the Grid
        foreach (var child in parentGrid.Children)
        {
            // Check if the child control is a Button
            if (child is Button childButton && int.TryParse(childButton.Tag.ToString(), out int childRatingValue))
            {
                // Change the color of each button which has the same or lesser value of what's being hovered or clicked on
                if (childRatingValue <= RatingValue)
                {
                    childButton.Foreground = (SolidColorBrush)FindResource("ThirnaryBrush");
                }
                else
                {
                    childButton.Foreground = (SolidColorBrush)FindResource("ThirnaryMidBrush");
                }
            }
        }
    }
}
