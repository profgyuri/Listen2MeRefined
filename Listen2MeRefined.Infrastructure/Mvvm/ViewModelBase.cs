using Listen2MeRefined.Infrastructure.Mvvm.Utils;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public abstract class ViewModelBase : ObservableObject, IAsyncInitializable
{
    private readonly object _gate = new();
    private Task? _initializeTask;

    public Task InitializeAsync(CancellationToken ct = default)
    {
        lock (_gate)
            return _initializeTask ??= InitializeCoreAsync(ct);
    }

    protected virtual Task InitializeCoreAsync(CancellationToken ct) => Task.CompletedTask;
}