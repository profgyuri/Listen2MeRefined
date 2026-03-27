namespace Listen2MeRefined.Application.Startup;

public interface IStartupTask
{
    Task RunAsync(CancellationToken ct);
}