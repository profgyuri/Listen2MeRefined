using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Startup;
using Listen2MeRefined.Infrastructure.Media;

namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class AudioOutputStartupTask : IStartupTask
{
    private readonly IOutputDevice _outputDevice;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public AudioOutputStartupTask(
        IOutputDevice outputDevice,
        ISettingsManager<AppSettings> settingsManager,
        IMediator mediator,
        ILogger logger)
    {
        _outputDevice = outputDevice;
        _settingsManager = settingsManager;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var selectedOutputDevice = await Task.Run(() =>
        {
            var allDevices = _outputDevice.EnumerateOutputDevices().ToArray();
            return allDevices.FirstOrDefault(x => x.Name == _settingsManager.Settings.AudioOutputDeviceName)
                   ?? allDevices.FirstOrDefault();
        }, ct).ConfigureAwait(false);

        _logger.Information("[AudioOutputStartupTask] Selected audio output device: {DeviceName}.",
            selectedOutputDevice?.Name ?? "None");

        if (selectedOutputDevice is null)
        {
            return;
        }

        await _mediator.Publish(new AudioOutputDeviceChangedNotification(selectedOutputDevice), ct)
            .ConfigureAwait(false);

        _logger.Debug("[AudioOutputStartupTask] Audio output device notification published.");
    }
}