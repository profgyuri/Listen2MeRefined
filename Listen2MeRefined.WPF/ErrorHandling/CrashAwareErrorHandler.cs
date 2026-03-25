using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.WPF.ErrorHandling;

public sealed class CrashAwareErrorHandler : IErrorHandler
{
    private readonly ICrashDialogService _crashDialogService;
    private readonly ILogLocationService _logLocationService;
    private readonly ILogger _logger;
    private static int _isHandlingFatal;

    public CrashAwareErrorHandler(
        ILogger logger,
        ICrashDialogService crashDialogService,
        ILogLocationService logLocationService)
    {
        _logger = logger.ForContext<CrashAwareErrorHandler>();
        _crashDialogService = crashDialogService;
        _logLocationService = logLocationService;
    }

    public Task HandleAsync(Exception exception, string context, CancellationToken cancellationToken = default)
    {
        _logger.Error(exception, "Unhandled exception in {Context}.", context);
        return Task.CompletedTask;
    }

    public async Task HandleUnhandledAsync(
        Exception exception,
        UnhandledErrorContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.Fatal(
            exception,
            "Unhandled fatal exception. Source={Source} IsTerminating={IsTerminating} OccurredAtUtc={OccurredAtUtc} Context={Context}",
            context.Source,
            context.IsTerminating,
            context.OccurredAtUtc,
            context.Context);

        if (Interlocked.Exchange(ref _isHandlingFatal, 1) == 1)
        {
            return;
        }

        try
        {
            var action = await _crashDialogService
                .ShowAsync(exception, context, cancellationToken)
                .ConfigureAwait(false);

            if (action == CrashDialogAction.OpenLogsAndExit)
            {
                try
                {
                    _logLocationService.OpenLogDirectory();
                }
                catch (Exception openLogsException)
                {
                    _logger.Error(
                        openLogsException,
                        "Failed to open log directory {LogDirectoryPath}.",
                        _logLocationService.LogDirectoryPath);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Warning("Unhandled exception reporting was canceled.");
        }
        catch (Exception dialogException)
        {
            _logger.Error(dialogException, "Unhandled exception reporting failed.");
        }
    }
}
