using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Core.DomainObjects;
using NAudio.CoreAudioApi;

namespace Listen2MeRefined.Infrastructure.Media;

public class NAudioOutputDevices : IOutputDevice
{
    private readonly ILogger _logger;

    public NAudioOutputDevices(ILogger logger)
    {
        _logger = logger;
    }

    public IEnumerable<AudioOutputDevice> EnumerateOutputDevices()
    {
        var deviceEnumerator = new MMDeviceEnumerator();

        _logger.Debug("[NAudioOutputDevices] Starting to enumerate audio devices at {@Time}", DateTimeOffset.Now);
        var devices = deviceEnumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(x => x.FriendlyName)
            .ToList();
        _logger.Debug("[NAudioOutputDevices] Got the full list of audio devices at {@Time}", DateTimeOffset.Now);

        var def = deviceEnumerator
            .GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
            .FriendlyName;

        yield return new AudioOutputDevice(-1, "Windows Default");

        devices.Remove(def);
        devices.Insert(0, def);

        for (var i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            yield return new AudioOutputDevice(i, device);
        }

        _logger.Debug("[NAudioOutputDevices] Finished enumerating audio devices at {@Time}", DateTimeOffset.Now);
    }
}