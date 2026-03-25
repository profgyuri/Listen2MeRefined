using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Startup;

namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class AudioOutputStartupTask : IStartupTask
{
    private readonly IOutputDevice _outputDevice;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly ILogger _logger;
    private readonly IMessenger _messenger;

    public AudioOutputStartupTask(
        IOutputDevice outputDevice,
        ISettingsManager<AppSettings> settingsManager,
        ILogger logger, 
        IMessenger messenger)
    {
        _outputDevice = outputDevice;
        _settingsManager = settingsManager;
        _logger = logger;
        _messenger = messenger;
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

        _messenger.Send(new AudioOutputDeviceChangedMessage(selectedOutputDevice));

        _logger.Debug("[AudioOutputStartupTask] Audio output device notification published.");
    }
}