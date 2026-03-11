using Listen2MeRefined.Application.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF;

using Listen2MeRefined.WPF.Views;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

internal static class WindowManager
{
    private static IServiceProvider _services = null!;

    internal static void Initialize(IServiceProvider services)
        => _services = services;
    
    /// <summary>
    ///     Shows a window registered in the dependency framework.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="isModal"></param>
    /// <returns></returns>
    internal static async Task<bool?> ShowWindowAsync<T>(
    double left,
    double top,
    bool isModal = true,
    CancellationToken ct = default)
    where T : Window
    {
        var window = _services.GetRequiredService<T>();

        window.Left = left - window.Width / 2;
        window.Top  = top  - window.Height / 2;

        if (window.DataContext is IInitializeAsync init)
            await init.InitializeAsync(ct); // keep on UI thread

        if (isModal)
        {
            var result = window.ShowDialog();
            return result;
        }

        window.Show();
        return null;
    }

    /// <summary>
    ///     Shows the New Song Window when the mouse coordinates are in a corner.
    /// </summary>
    /// <param name="x">X parameter of mouse position.</param>
    /// <param name="y">Y parameter of mouse position.</param>
    /// <returns>The instance of the New Song Window.</returns>
    internal static NewSongWindow ShowNewSongWindow(
        int x,
        int y,
        int triggerAreaSize = 10)
    {
        var window = _services.GetRequiredService<NewSongWindow>();

        if (x <= triggerAreaSize)
        {
            window.Left = 0;
        }
        else if (x >= SystemParameters.PrimaryScreenWidth - triggerAreaSize)
        {
            window.Left = SystemParameters.PrimaryScreenWidth - window.Width;
        }
        else
        {
            window.Left = x <= SystemParameters.PrimaryScreenWidth / 2
                ? 0
                : SystemParameters.PrimaryScreenWidth - window.Width;
        }

        if (y <= triggerAreaSize)
        {
            window.Top = 0;
        }
        else if (y >= SystemParameters.PrimaryScreenHeight - triggerAreaSize)
        {
            window.Top = SystemParameters.WorkArea.Height - window.Height;
        }
        else
        {
            window.Top = y <= SystemParameters.PrimaryScreenHeight / 2
                ? 0
                : SystemParameters.WorkArea.Height - window.Height;
        }

        window.Show();
        return window;
    }

    /// <summary>
    ///     Closes the new song window, when the mouse coordinates are no longer in a corner.
    /// </summary>
    internal static void CloseNewSongWindow(NewSongWindow? window)
    {
        window?.Hide();
    }
}
