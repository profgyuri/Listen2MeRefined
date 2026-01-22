namespace Listen2MeRefined.Infrastructure.Mvvm;

public abstract class ViewModelBase : ObservableObject, IAsyncInitializable
{
    private int _initialized; // 0 = no, 1 = yes

    public Task InitializeAsync(CancellationToken ct = default)
        => Interlocked.Exchange(ref _initialized, 1) == 1
            ? Task.CompletedTask
            : InitializeCoreAsync(ct);

    /// <summary>
    /// This method should handle the actual initialization logic.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    protected virtual Task InitializeCoreAsync(CancellationToken ct)
        => Task.CompletedTask;
}