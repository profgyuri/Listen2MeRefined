using CommunityToolkit.Mvvm.ComponentModel;

namespace Listen2MeRefined.Application.ViewModels;

public abstract class ViewModelBase : ObservableObject, IAsyncInitializable
{
    private readonly Lock _gate = new();
    private Task? _initializeTask;

    public Task InitializeAsync(CancellationToken ct = default)
    {
        lock (_gate)
            return _initializeTask ??= InitializeCoreAsync(ct);
    }

    protected virtual Task InitializeCoreAsync(CancellationToken ct) => Task.CompletedTask;
}