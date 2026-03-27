using System.Runtime.CompilerServices;
using Listen2MeRefined.Application.ViewModels;

namespace Listen2MeRefined.Application.Navigation;

/// <summary>
/// Provides per-instance one-time asynchronous initialization coordination.
/// </summary>
public sealed class InitializationTracker : IInitializationTracker
{
    private readonly ConditionalWeakTable<IInitializeAsync, Gate> _gates = new();

    /// <inheritdoc />
    public async Task EnsureInitializedAsync(IInitializeAsync instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (instance is ViewModelBase viewModelBase)
        {
            await viewModelBase.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var gate = _gates.GetValue(instance, _ => new Gate());
        if (Volatile.Read(ref gate.IsInitialized) == 1)
        {
            return;
        }

        await gate.Sync.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref gate.IsInitialized) == 1)
            {
                return;
            }

            await instance.InitializeAsync(cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref gate.IsInitialized, 1);
        }
        finally
        {
            gate.Sync.Release();
        }
    }

    private sealed class Gate
    {
        public SemaphoreSlim Sync { get; } = new(1, 1);

        public int IsInitialized;
    }
}
