using Listen2MeRefined.Application.ViewModels;

namespace Listen2MeRefined.Application.Navigation;

/// <summary>
/// Ensures one-time asynchronous initialization per object instance.
/// </summary>
public interface IInitializationTracker
{
    /// <summary>
    /// Ensures initialization runs once for the specified instance.
    /// </summary>
    /// <param name="instance">The initializable instance.</param>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A task representing the initialization operation.</returns>
    Task EnsureInitializedAsync(IInitializeAsync instance, CancellationToken cancellationToken = default);
}