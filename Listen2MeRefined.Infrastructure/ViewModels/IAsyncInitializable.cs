namespace Listen2MeRefined.Infrastructure.ViewModels;

public interface IAsyncInitializable
{
    Task InitializeAsync(CancellationToken ct = default);
}