using Listen2MeRefined.Infrastructure.Mvvm.Utils;

namespace Listen2MeRefined.WPF.Utils;

using System;
using System.Threading.Tasks;
using System.Windows;

public static class ViewModelInitialization
{
    public static readonly DependencyProperty AutoInitializeProperty =
        DependencyProperty.RegisterAttached(
            "AutoInitialize",
            typeof(bool),
            typeof(ViewModelInitialization),
            new PropertyMetadata(false, OnAutoInitializeChanged));

    public static void SetAutoInitialize(DependencyObject element, bool value)
        => element.SetValue(AutoInitializeProperty, value);

    public static bool GetAutoInitialize(DependencyObject element)
        => (bool)element.GetValue(AutoInitializeProperty);

    private static void OnAutoInitializeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe)
            return;

        if ((bool)e.NewValue)
        {
            fe.Loaded += OnLoaded;
            fe.DataContextChanged += OnDataContextChanged;
        }
        else
        {
            fe.Loaded -= OnLoaded;
            fe.DataContextChanged -= OnDataContextChanged;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var fe = (FrameworkElement)sender;
        _ = TryInitializeAsync(fe);
    }

    private static void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var fe = (FrameworkElement)sender;
        _ = TryInitializeAsync(fe);
    }

    private static async Task TryInitializeAsync(FrameworkElement fe)
    {
        // Ensure we only run when the view is actually in the tree
        if (!fe.IsLoaded)
            return;

        if (fe.DataContext is not IAsyncInitializable init)
            return;

        try
        {
            await init.InitializeAsync();
        }
        catch (Exception ex)
        {
            // Centralize your policy: log, show error, etc.
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
