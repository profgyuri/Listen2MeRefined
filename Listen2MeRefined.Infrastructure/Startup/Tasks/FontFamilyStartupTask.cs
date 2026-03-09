using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Startup;

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
        var fontFamily = _settingsManager.Settings.FontFamily;

        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            return;
        }
        
        await _mediator.Publish(new FontFamilyChangedNotification(fontFamily), ct)
            .ConfigureAwait(false);

        _logger.Debug("[FontFamilyStartupTask] Font family notification published with value {FontFamily}.",
            fontFamily);
    }
}
