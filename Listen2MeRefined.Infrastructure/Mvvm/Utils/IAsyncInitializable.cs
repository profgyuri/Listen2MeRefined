namespace Listen2MeRefined.Infrastructure.Mvvm.Utils;

public interface IAsyncInitializable
{
    Task InitializeAsync(CancellationToken ct = default);
}