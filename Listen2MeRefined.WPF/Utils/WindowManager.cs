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
        else
        {
            window.Show();
            return null;
        }
    }
}