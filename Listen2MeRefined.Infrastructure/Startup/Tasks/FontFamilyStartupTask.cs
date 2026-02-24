using Listen2MeRefined.Infrastructure.Notifications;

namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class FontFamilyStartupTask : IStartupTask
{
    private readonly IMediator _mediator;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly ILogger _logger;

    public FontFamilyStartupTask(
        IMediator mediator,
        ISettingsManager<AppSettings> settingsManager,
        ILogger logger)
    {
        _mediator = mediator;
        _settingsManager = settingsManager;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await _mediator.Publish(new FontFamilyChangedNotification(_settingsManager.Settings.FontFamily), ct)
            .ConfigureAwait(false);

        _logger.Debug("[FontFamilyStartupTask] Font family notification published with value {FontFamily}.",
            _settingsManager.Settings.FontFamily);
    }
}
