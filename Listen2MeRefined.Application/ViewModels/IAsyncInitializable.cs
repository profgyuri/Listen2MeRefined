namespace Listen2MeRefined.Application.ViewModels;

public interface IAsyncInitializable
{
    Task InitializeAsync(CancellationToken ct = default);
}