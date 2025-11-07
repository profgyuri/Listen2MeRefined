namespace Listen2MeRefined.Infrastructure.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;

internal static class AudioDevices
{
    internal static IEnumerable<AudioOutputDevice> GetOutputDevices()
    {
        var deviceEnumerator = new MMDeviceEnumerator();
        var devices = deviceEnumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(x => x.FriendlyName)
            .ToList();
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
    }
}