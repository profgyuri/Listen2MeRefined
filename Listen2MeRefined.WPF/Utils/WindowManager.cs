using Listen2MeRefined.WPF.Views;

namespace Listen2MeRefined.WPF;

using Autofac;
using System.Windows;

internal static class WindowManager
{
    private const int TriggerNotificationWindowAreaSize = 10;
    
    /// <summary>
    ///     Shows a window registered in the dependency framework.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="isModal">Set to <see langword="true"/> if the method should return only after closing the window, 
    /// set to <see langword="false"/> to return immediately.</param>
    internal static bool? ShowWindow<T>(bool isModal = true)
        where T : Window
    {
        using var scope = IocContainer.GetContainer().BeginLifetimeScope();
        var window = scope.Resolve<T>();
        
        if (isModal)
        {
            var result = window.ShowDialog();
            return result;
        }

        window.Show();
        return null;
    }

    /// <summary>
    ///     Shows a window registered in the dependency framework.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="isModal"></param>
    /// <returns></returns>
    internal static bool? ShowWindow<T>(double left, double top, bool isModal = true)
        where T : Window
    {
        using var scope = IocContainer.GetContainer().BeginLifetimeScope();
        var window = scope.Resolve<T>();

        window.Left = left - window.Width / 2;
        window.Top = top - window.Height / 2;

        if (isModal)
        {
            var result = window.ShowDialog();
            return result;
        }

        window.Show();
        return null;
    }

    /// <summary>
    /// Shows the New Song Window when the mouse coordinates are in a corner.
    /// </summary>
    /// <param name="x">X parameter of mouse position.</param>
    /// <param name="y">Y parameter of mouse position.</param>
    /// <returns>The instance of the New Song Window.</returns>
    internal static NewSongWindow ShowNewSongWindow(int x, int y)
    {
        using var scope = IocContainer.GetContainer().BeginLifetimeScope();
        var window = scope.Resolve<NewSongWindow>();

        if (x <= TriggerNotificationWindowAreaSize)
        {
            window.Left = 0;
        }
        else if (x >= SystemParameters.PrimaryScreenWidth - TriggerNotificationWindowAreaSize)
        {
            window.Left = SystemParameters.PrimaryScreenWidth - window.Width;
        }
        
        if (y <= TriggerNotificationWindowAreaSize)
        {
            window.Top = 0;
        }
        else if (y >= SystemParameters.PrimaryScreenHeight - TriggerNotificationWindowAreaSize)
        {
            window.Top = SystemParameters.WorkArea.Height - window.Height;
        }
        
        window.Show();
        return window;
    }
    
    /// <summary>
    /// Closes the new song window, when the mouse coordinates are no longer in a corner.
    /// </summary>
    internal static void CloseNewSongWindow(NewSongWindow window)
    {
        window.Close();
    }
}