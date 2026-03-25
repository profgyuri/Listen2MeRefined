using Listen2MeRefined.Application.ErrorHandling;

namespace Listen2MeRefined.WPF.ErrorHandling;

public interface ICrashDialogService
{
    Task<CrashDialogAction> ShowAsync(
        Exception exception,
        UnhandledErrorContext context,
        CancellationToken cancellationToken = default);
}
