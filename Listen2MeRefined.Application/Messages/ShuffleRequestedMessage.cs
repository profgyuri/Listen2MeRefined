using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class ShuffleRequestedMessage() : ValueChangedMessage<bool>(true);
