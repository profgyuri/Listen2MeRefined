using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class CornerWindowPositionChangedMessage(string position)
    : ValueChangedMessage<string>(position);
