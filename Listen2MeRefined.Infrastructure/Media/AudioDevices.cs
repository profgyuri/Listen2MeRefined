using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media;

internal static class AudioDevices
{
    internal static IEnumerable<AudioOutputDevice> GetOutputDevices()
    {
        var list = new List<AudioOutputDevice>();
        var deviceEnumerator = new MMDeviceEnumerator();
        var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        list.Add(new AudioOutputDevice(-1, "Windows Default"));

        for (var i = 0; i < WaveOut.DeviceCount; i++)
        {
            var caps = WaveOut.GetCapabilities(i);
            var name = devices.First(x => x.FriendlyName.StartsWith(caps.ProductName)).FriendlyName;
            list.Add(new AudioOutputDevice(i, name));
        }

        return list;
    }
}