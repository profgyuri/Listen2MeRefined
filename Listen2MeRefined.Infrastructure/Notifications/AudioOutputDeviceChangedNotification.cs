using Listen2MeRefined.Core.DomainObjects;

namespace Listen2MeRefined.Infrastructure.Notifications;

public class AudioOutputDeviceChangedNotification : INotification
{
    public AudioOutputDevice Device { get; set; }

    public AudioOutputDeviceChangedNotification(AudioOutputDevice device)
    {
        Device = device;
    }
}