using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Listen2MeRefined.Application.ErrorHandling;
using System.Windows.Media;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels;
using Listen2MeRefined.Application.ViewModels.Shells;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Listen2MeRefined.WPF.Utils.Navigation;

public sealed class WindowManager : IWindowManager
{
    private readonly IErrorHandler _errorHandler;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IUiDispatcher _ui;
    private readonly IWindowRegistry _windowRegistry;

    /// <summary>
    /// Live window registry keyed by shell ViewModel reference.
    /// </summary>
    private readonly ConcurrentDictionary<object, WindowDescriptor> _openWindows = new(ReferenceEqualityComparer.Instance);

    public WindowManager(
        IErrorHandler errorHandler,
        IServiceProvider serviceProvider,
        ILogger logger,
        IUiDispatcher ui,
        IWindowRegistry windowRegistry)
    {
        _errorHandler = errorHandler;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _ui = ui;
        _windowRegistry = windowRegistry;
    }

    public async Task ShowMainWindowAsync<TShellViewModel>(
        CancellationToken cancellationToken = default)
        where TShellViewModel : ShellViewModelBase
    {
        _logger.Information(
            "Showing main window for ShellVM={ShellVMType}", typeof(TShellViewModel).Name);

        async Task ShowMainWindowCoreAsync()
        {
            var (window, shellVm, context) = BuildWindow<TShellViewModel>();

            System.Windows.Application.Current.MainWindow = (Window)window;

            Register(window, shellVm, context);
            ((Window)window).Show();

            await RunInitializationAsync(shellVm, context, (Window)window, cancellationToken);
        }

        await _ui.InvokeAsync(ShowMainWindowCoreAsync, cancellationToken);
    }

    public async Task<bool?> ShowWindowAsync<TShellViewModel>(
        WindowShowOptions options,
        CancellationToken cancellationToken = default)
        where TShellViewModel : ShellViewModelBase
    {
        _logger.Information(
            "Showing window for ShellVM={ShellVMType} Modal={IsModal}",
            typeof(TShellViewModel).Name, options.IsModal);

        bool? dialogResult = null;

        async Task ShowWindowCoreAsync()
        {
            var (window, shellVm, context) = BuildWindow<TShellViewModel>();
            var wpfWindow = (Window)window;

            ApplyPosition(wpfWindow, options);
            Register(window, shellVm, context);

            await RunInitializationAsync(shellVm, context, wpfWindow, cancellationToken);

            if (options.IsModal)
            {
                dialogResult = wpfWindow.ShowDialog();
            }
            else
            {
                wpfWindow.Show();
            }
        }

        await _ui.InvokeAsync(ShowWindowCoreAsync, cancellationToken);
        return dialogResult;
    }

    public async Task<bool?> ShowPopupAsync<TPopupViewModel>(
        WindowShowOptions options,
        Action<TPopupViewModel>? configureViewModel = null,
        CancellationToken cancellationToken = default)
        where TPopupViewModel : PopupViewModelBase
    {
        _logger.Information(
            "Showing popup for PopupVM={PopupVMType} Modal={IsModal}",
            typeof(TPopupViewModel).Name, options.IsModal);

        bool? dialogResult = null;

        async Task ShowPopupCoreAsync()
        {
            var (window, shellVm, context) = BuildWindow<PopupShellViewModel>();
            var wpfWindow = (Window)window;

            ApplyPosition(wpfWindow, options);
            Register(window, shellVm, context);

            await RunInitializationAsync(shellVm, context, wpfWindow, cancellationToken);
            await shellVm.NavigateToAsync<TPopupViewModel>(cancellationToken);

            if (shellVm.CurrentViewModel is not TPopupViewModel popupViewModel)
            {
                throw new InvalidOperationException(
                    $"Popup shell failed to navigate to {typeof(TPopupViewModel).Name}.");
            }

            configureViewModel?.Invoke(popupViewModel);

            if (options.IsModal)
            {
                dialogResult = wpfWindow.ShowDialog();
            }
            else
            {
                wpfWindow.Show();
            }
        }

        await _ui.InvokeAsync(ShowPopupCoreAsync, cancellationToken);
        return dialogResult;
    }

    public void CloseWindow<TShellViewModel>() where TShellViewModel : ShellViewModelBase
    {
        var descriptor = _openWindows.Values
            .FirstOrDefault(d => d.ShellViewModel is TShellViewModel);

        if (descriptor is null)
        {
            return;
        }

        _ui.InvokeAsync(() => ((Window)descriptor.Window).Close());
    }

    public bool IsOpen<TShellViewModel>() where TShellViewModel : ShellViewModelBase
        => _openWindows.Values.Any(d => d.ShellViewModel is TShellViewModel);

    private (object window, TShellViewModel shellVm, ShellContext context) BuildWindow<TShellViewModel>()
        where TShellViewModel : ShellViewModelBase
    {
        var shellVm = _serviceProvider.GetRequiredService<TShellViewModel>();
        var context = shellVm.ShellContext;

        var windowType = _windowRegistry.Resolve<TShellViewModel>();
        var window = (Window)ActivatorUtilities.CreateInstance(_serviceProvider, windowType, shellVm);
        window.DataContext = shellVm;

        return (window, shellVm, context);
    }

    private void Register(object window, object shellVm, ShellContext context)
    {
        var descriptor = new WindowDescriptor(window, shellVm, context);
        _openWindows[shellVm] = descriptor;

        ((Window)window).Closed += (_, _) =>
        {
            _openWindows.TryRemove(shellVm, out _);
            _logger.Information(
                "Window closed and unregistered. ShellVM={ShellVMType}",
                shellVm.GetType().Name);

            if (shellVm is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
    }

    /// <summary>
    /// Runs async initialization on the shell ViewModel (and therefore its
    /// current content ViewModel) after the window is shown.  Closes the
    /// window and logs on failure.
    /// </summary>
    private async Task RunInitializationAsync(
        object shellVm,
        ShellContext context,
        Window window,
        CancellationToken cancellationToken)
    {
        if (shellVm is not IInitializeAsync initializable)
        {
            return;
        }

        try
        {
            await context.InitializationTracker
                .EnsureInitializedAsync(initializable, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning(
                "Initialization was cancelled for {ShellVM}.", shellVm.GetType().Name);

            await _ui.InvokeAsync(window.Close, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "Initialization failed for {ShellVM}. The window will be closed.",
                shellVm.GetType().Name);

            var errorContext = new UnhandledErrorContext(
                UnhandledErrorSource.WindowInitialization,
                IsTerminating: false,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Context: shellVm.GetType().Name);

            try
            {
                await _errorHandler
                    .HandleUnhandledAsync(ex, errorContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception errorHandlerException)
            {
                _logger.Error(
                    errorHandlerException,
                    "Error handler failed while processing window initialization failure for {ShellVM}.",
                    shellVm.GetType().Name);
            }
            finally
            {
                await _ui.InvokeAsync(window.Close, CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Positions <paramref name="window"/> according to <paramref name="options"/>.
    /// </summary>
    private static void ApplyPosition(Window window, WindowShowOptions options)
    {
        if (options.CentreOnMainWindow && System.Windows.Application.Current.MainWindow is { } main)
        {
            // Defer until layout pass so Width/Height are known.
            window.SourceInitialized += (_, _) =>
            {
                window.Left = main.Left + (main.Width - window.ActualWidth) / 2;
                window.Top = main.Top + (main.Height - window.ActualHeight) / 2;
            };
            return;
        }

        if (options.Left.HasValue && options.Top.HasValue)
        {
            window.SourceInitialized += (_, _) =>
            {
                var anchorX = options.Left.Value;
                var anchorY = options.Top.Value;
                var anchorPoint = ConvertFromDevicePixels(window, anchorX, anchorY);
                anchorX = anchorPoint.X;
                anchorY = anchorPoint.Y;

                switch (options.Anchor)
                {
                    case WindowPositionAnchor.TopLeft:
                        window.Left = anchorX;
                        window.Top = anchorY;
                        break;
                    case WindowPositionAnchor.TopRight:
                        window.Left = anchorX - window.ActualWidth;
                        window.Top = anchorY;
                        break;
                    case WindowPositionAnchor.BottomLeft:
                        window.Left = anchorX;
                        window.Top = anchorY - window.ActualHeight;
                        break;
                    case WindowPositionAnchor.BottomRight:
                        window.Left = anchorX - window.ActualWidth;
                        window.Top = anchorY - window.ActualHeight;
                        break;
                    case WindowPositionAnchor.Center:
                    default:
                        window.Left = anchorX - window.ActualWidth / 2;
                        window.Top = anchorY - window.ActualHeight / 2;
                        break;
                }
            };
        }
    }

    private static System.Windows.Point ConvertFromDevicePixels(Window window, double deviceX, double deviceY)
    {
        var source = PresentationSource.FromVisual(window);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        return transform.Transform(new System.Windows.Point(deviceX, deviceY));
    }
}

