namespace Listen2MeRefined.WPF;

using System;
using System.Globalization;
using System.Windows.Data;

/// <summary>
///     Value converter made for settings window to determine
///     if there is any folder selected for removal. <para/>
///     Used to enable or disable the Remove Folder button.
/// </summary>
internal class IsFolderSelectedConverter : IValueConverter
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