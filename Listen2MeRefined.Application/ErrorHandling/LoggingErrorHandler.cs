using Serilog;

namespace Listen2MeRefined.Application.ErrorHandling;

/// <summary>
/// Logs recoverable errors through the configured logging pipeline.
/// </summary>
public sealed class LoggingErrorHandler : IErrorHandler
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingErrorHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger used to emit error entries.</param>
    public LoggingErrorHandler(ILogger logger)
    {
        _logger = logger.ForContext<LoggingErrorHandler>();
    }

    /// <inheritdoc />
    public Task HandleAsync(Exception exception, string context, CancellationToken cancellationToken = default)
    {
        _logger.Error(exception, "Unhandled exception in {Context}.", context);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleUnhandledAsync(
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

        return Task.CompletedTask;
    }
}
