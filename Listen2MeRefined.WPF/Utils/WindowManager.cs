using Listen2MeRefined.WPF.Views;

namespace Listen2MeRefined.WPF;

using Autofac;
using System.Windows;

internal static class WindowManager
{
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
    internal static void ShowNewSongWindow(int x, int y)
    {
        using var scope = IocContainer.GetContainer().BeginLifetimeScope();
        var window = scope.Resolve<NewSongWindow>();

        if (x <= 5)
        {
            window.Left = 0;
        }
        else if (x >= SystemParameters.PrimaryScreenWidth - 5)
        {
            window.Left = SystemParameters.PrimaryScreenWidth - window.Width;
        }
        
        if (y <= 5)
        {
            window.Top = 0;
        }
        else if (y >= SystemParameters.PrimaryScreenHeight - 5)
        {
            window.Top = SystemParameters.PrimaryScreenHeight - window.Height;
        }
        
        window.Show();
    }
}