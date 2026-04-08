using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Application.Utils;

namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class GlobalHookStartupTask : IStartupTask
{
    private readonly IGlobalHook _globalHook;
    private readonly ILogger _logger;

    public GlobalHookStartupTask(
        IGlobalHook globalHook,
        ILogger logger)
    {
        _globalHook = globalHook;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.Information("[GlobalHookStartupTask] Registering global hooks...");
        await _globalHook.RegisterAsync(ct).ConfigureAwait(false);
        _logger.Information("[GlobalHookStartupTask] Global hooks registered.");
    }
}
