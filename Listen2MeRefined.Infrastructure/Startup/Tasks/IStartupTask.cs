namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public interface IStartupTask
{
    Task RunAsync(CancellationToken ct);
}