using Listen2MeRefined.Core.DomainObjects;
using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public class AudioOutputDeviceChangedNotification : INotification
{
    public AudioOutputDevice Device { get; set; }

    public AudioOutputDeviceChangedNotification(AudioOutputDevice device)
    {
        Device = device;
    }
}