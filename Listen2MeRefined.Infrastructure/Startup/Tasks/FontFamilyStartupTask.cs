using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Startup;

namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class FontFamilyStartupTask : IStartupTask
{
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly ILogger _logger;
    private readonly IMessenger _messenger;

    public FontFamilyStartupTask(
        ISettingsManager<AppSettings> settingsManager,
        ILogger logger, 
        IMessenger messenger)
    {
        _settingsManager = settingsManager;
        _logger = logger;
        _messenger = messenger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var fontFamily = _settingsManager.Settings.FontFamily;

        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            return;
        }
        
        _messenger.Send(new FontFamilyChangedMessage(fontFamily));

        _logger.Debug("[FontFamilyStartupTask] Font family notification published with value {FontFamily}.",
            fontFamily);
    }
}
