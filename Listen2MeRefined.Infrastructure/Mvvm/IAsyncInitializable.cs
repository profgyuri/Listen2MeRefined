namespace Listen2MeRefined.Infrastructure.Mvvm;

public interface IAsyncInitializable
{
    Task InitializeAsync(CancellationToken ct = default);
}