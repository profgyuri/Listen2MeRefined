using System.Windows.Media;

namespace Listen2MeRefined.WPF;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
///     Value converter made for settings window to determine
///     if there is any folder selected for removal.
///     <para />
///     Used to enable or disable the Remove Folder button.
/// </summary>
internal sealed class IsFolderSelectedConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return !string.IsNullOrEmpty((string) value);
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

internal sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return (bool) value ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

internal sealed class TrimmedTextBlockVisibilityConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value == null)
        {
            return Visibility.Collapsed;
        }

        var textBlock = (FrameworkElement) value;

        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        return ((FrameworkElement) value).ActualWidth < ((FrameworkElement) value).DesiredSize.Width
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class StringToFontFamilyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string name && !string.IsNullOrWhiteSpace(name)
            ? new FontFamily(name)
            : new FontFamily("Segoe UI");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is FontFamily ff ? ff.Source : "Segoe UI";
}

/// <summary>
///     Compares two string values for case-insensitive equality.
///     Used to highlight the currently playing song in a playlist by comparing
///     each item's path against the active song path held on the view model.
/// </summary>
public sealed class PathEqualsMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
        {
            return false;
        }

        var itemPath = values[0] as string;
        var activePath = values[1] as string;

        if (string.IsNullOrEmpty(itemPath) || string.IsNullOrEmpty(activePath))
        {
            return false;
        }

        return string.Equals(itemPath, activePath, StringComparison.OrdinalIgnoreCase);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}