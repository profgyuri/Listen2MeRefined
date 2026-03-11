namespace Listen2MeRefined.Application.ErrorHandling;

/// <summary>
/// Handles recoverable runtime errors in UI workflows.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Handles an exception with a contextual label.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="context">A short context describing where the exception occurred.</param>
    /// <param name="cancellationToken">A token that can cancel the handling operation.</param>
    /// <returns>A task representing the asynchronous handling operation.</returns>
    Task HandleAsync(Exception exception, string context, CancellationToken cancellationToken = default);
}
