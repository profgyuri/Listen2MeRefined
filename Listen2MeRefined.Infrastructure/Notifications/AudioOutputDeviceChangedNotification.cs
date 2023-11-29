namespace Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

public class AudioOutputDeviceChangedNotification : INotification
{
    public AudioOutputDevice Device { get; set; }

    public AudioOutputDeviceChangedNotification(AudioOutputDevice device)
    {
        Device = device;
    }
}