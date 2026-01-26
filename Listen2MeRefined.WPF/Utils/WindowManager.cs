using Listen2MeRefined.Infrastructure.Mvvm.Utils;

namespace Listen2MeRefined.WPF;
using Autofac;
using Listen2MeRefined.WPF.Dependency;
using Listen2MeRefined.WPF.Views;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

internal static class WindowManager
{
    private const int TriggerNotificationWindowAreaSize = 10;

    /// <summary>
    ///     Shows a window registered in the dependency framework.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static void ShowMainWindow<T>()
        where T : Window
    {
        var scope = IocContainer.GetContainer().BeginLifetimeScope();

        try
        {
            var window = scope.Resolve<T>();

            Application.Current.MainWindow = window;
            window.Closed += (_, __) => scope.Dispose();

            if (window.DataContext is IAsyncInitializable init)
            {
                async void Handler(object? sender, EventArgs e)
                {
                    window.ContentRendered -= Handler;

                    try
                    {
                        await init.InitializeAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Initialization failed");
                        window.Close();
                    }
                }

                window.ContentRendered += Handler;
            }

            window.Show();
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

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
    var scope = IocContainer.GetContainer().BeginLifetimeScope();

    try
    {
        var window = scope.Resolve<T>();

        window.Left = left - window.Width / 2;
        window.Top  = top  - window.Height / 2;

        // Ensure scope survives for modeless windows
        window.Closed += (_, __) => scope.Dispose();

        if (window.DataContext is IAsyncInitializable init)
            await init.InitializeAsync(ct); // keep on UI thread

        if (isModal)
        {
            var result = window.ShowDialog();
            return result;
        }

        window.Show();
        return null;
    }
    catch
    {
        scope.Dispose();
        throw;
    }
}

    /// <summary>
    ///     Shows the New Song Window when the mouse coordinates are in a corner.
    /// </summary>
    /// <param name="x">X parameter of mouse position.</param>
    /// <param name="y">Y parameter of mouse position.</param>
    /// <returns>The instance of the New Song Window.</returns>
    internal static NewSongWindow ShowNewSongWindow(
        int x,
        int y)
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
    ///     Closes the new song window, when the mouse coordinates are no longer in a corner.
    /// </summary>
    internal static void CloseNewSongWindow(NewSongWindow window)
    {
        window?.Hide();
    }
}