namespace Listen2MeRefined.Application.Startup;

public interface IStartupManager
{
    public Task StartAsync(CancellationToken ct = default);
}