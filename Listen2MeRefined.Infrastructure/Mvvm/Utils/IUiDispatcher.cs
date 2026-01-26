namespace Listen2MeRefined.Infrastructure.Mvvm.Utils;

public interface IUiDispatcher
{
    bool CheckAccess();
    Task InvokeAsync(Action action, CancellationToken ct = default);
    Task<T> InvokeAsync<T>(Func<T> func, CancellationToken ct = default);
}