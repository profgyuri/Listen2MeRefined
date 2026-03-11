namespace Listen2MeRefined.Application.ViewModels;

public interface IInitializeAsync
{
    Task InitializeAsync(CancellationToken ct = default);
}