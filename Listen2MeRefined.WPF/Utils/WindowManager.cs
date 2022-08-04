namespace Listen2MeRefined.WPF;

using Autofac;
using System.Windows;

internal static class WindowManager
{
    internal static void ShowWindow<T>()
        where T : Window
    {
        using var scope = IocContainer.GetContainer().BeginLifetimeScope();
        var window = scope.Resolve<T>();
        window.ShowDialog();
    }
}