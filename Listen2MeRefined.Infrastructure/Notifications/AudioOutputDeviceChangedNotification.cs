using MediatR;

namespace Listen2MeRefined.Infrastructure.Notifications;

public class AudioOutputDeviceChangedNotification : INotification
{
    public AudioOutputDevice DeviceIndex { get; set; }

    public AudioOutputDeviceChangedNotification(AudioOutputDevice deviceIndex)
    {
        DeviceIndex = deviceIndex;
    }
}