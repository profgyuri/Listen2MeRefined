using System.Windows;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Listen2MeRefined.WPF.Utils.Navigation;

internal class WindowManager : IWindowManager
{
    private readonly IServiceProvider _services;
    private readonly IShellContextFactory _shellContextFactory;

    public WindowManager(IServiceProvider services, IShellContextFactory shellContextFactory)
    {
        _services = services;
        _shellContextFactory = shellContextFactory;
    }
    
    /// <summary>
    /// Shows a window of type <typeparamref name="TWindow"/> with its own shell context,
    /// navigating to the specified route on open.
    /// </summary>
    public async Task<bool?> ShowWindowAsync<TWindow, TShellViewModel>(
        string initialRoute,
        double left,
        double top,
        bool isModal = true,
        CancellationToken ct = default)
        where TWindow : Window
        where TShellViewModel : ShellViewModelBase
    {
        var shellContext = _shellContextFactory.Create();
        var shellVm = ActivatorUtilities.CreateInstance<TShellViewModel>(_services, shellContext);
        var window = ActivatorUtilities.CreateInstance<TWindow>(_services, shellVm);

        window.Left = left - window.Width / 2;
        window.Top  = top  - window.Height / 2;

        await shellContext.NavigationService
            .NavigateAsync(initialRoute, cancellationToken: ct)
            .ConfigureAwait(true);

        if (isModal)
            return window.ShowDialog();

        window.Show();
        return null;
    }

    /// <summary>
    /// Shows the NewSongWindow in the nearest screen corner based on mouse position.
    /// </summary>
    public CornerWindow ShowCornerWindow(int x, int y, int triggerAreaSize = 10)
    {
        var window = _services.GetRequiredService<CornerWindow>();

        window.Left = x <= SystemParameters.PrimaryScreenWidth / 2
            ? 0
            : SystemParameters.PrimaryScreenWidth - window.Width;

        window.Top = y <= SystemParameters.PrimaryScreenHeight / 2
            ? 0
            : SystemParameters.WorkArea.Height - window.Height;

        if (x <= triggerAreaSize)
            window.Left = 0;
        else if (x >= SystemParameters.PrimaryScreenWidth - triggerAreaSize)
            window.Left = SystemParameters.PrimaryScreenWidth - window.Width;

        if (y <= triggerAreaSize)
            window.Top = 0;
        else if (y >= SystemParameters.PrimaryScreenHeight - triggerAreaSize)
            window.Top = SystemParameters.WorkArea.Height - window.Height;

        window.Show();
        return window;
    }

    /// <summary>
    ///     Closes the new song window when the mouse coordinates are no longer in a corner.
    /// </summary>
    public void CloseCornerWindow(CornerWindow? window) => window?.Hide();
}
