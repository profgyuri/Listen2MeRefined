using Listen2MeRefined.Application.Startup;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Listen2MeRefined.WPF.Startup;

public sealed class StartupHostedService : IHostedService
{
    private readonly IStartupManager _startupManager;
    private readonly ILogger _logger;

    public StartupHostedService(IStartupManager startupManager, ILogger logger)
    {
        _startupManager = startupManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Information("[StartupHostedService] Running startup pipeline...");
        await _startupManager.StartAsync(cancellationToken).ConfigureAwait(false);
        _logger.Information("[StartupHostedService] Startup pipeline completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
