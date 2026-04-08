using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class FocusSearchBarRequestedMessage() : ValueChangedMessage<bool>(true);
