namespace Listen2MeRefined.WPF;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
///     Value converter made for settings window to determine
///     if there is any folder selected for removal. <para/>
///     Used to enable or disable the Remove Folder button.
/// </summary>
internal sealed class IsFolderSelectedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty((string)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class TrimmedTextBlockVisibilityConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return Visibility.Collapsed;

        FrameworkElement textBlock = (FrameworkElement)value;

        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        if (((FrameworkElement)value).ActualWidth < ((FrameworkElement)value).DesiredSize.Width)
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}