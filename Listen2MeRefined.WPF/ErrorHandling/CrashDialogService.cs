using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.WPF.Views.Shells;
using Serilog;

namespace Listen2MeRefined.WPF.ErrorHandling;

public sealed class CrashDialogService : ICrashDialogService
{
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogLocationService _logLocationService;
    private readonly ILogger _logger;

    public CrashDialogService(
        IUiDispatcher uiDispatcher,
        ILogLocationService logLocationService,
        ILogger logger)
    {
        _uiDispatcher = uiDispatcher;
        _logLocationService = logLocationService;
        _logger = logger.ForContext<CrashDialogService>();
    }

    public async Task<CrashDialogAction> ShowAsync(
        Exception exception,
        UnhandledErrorContext context,
        CancellationToken cancellationToken = default)
    {
        CrashDialogAction action = CrashDialogAction.Exit;

        if (System.Windows.Application.Current is null)
        {
            _logger.Warning(
                "Skipping crash dialog because WPF application is unavailable. Source={Source}",
                context.Source);
            return action;
        }

        await _uiDispatcher.InvokeAsync(() =>
        {
            var window = new CrashReportWindow(exception, context, _logLocationService.LogDirectoryPath)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };

            _ = window.ShowDialog();
            action = window.Action;
        }, cancellationToken).ConfigureAwait(false);

        return action;
    }
}
