using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Core.DomainObjects;

namespace Listen2MeRefined.Application.Messages;

public sealed class AudioOutputDeviceChangedMessage(AudioOutputDevice device)
    : ValueChangedMessage<AudioOutputDevice>(device);
