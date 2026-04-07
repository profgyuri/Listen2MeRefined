using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public class SearchDebounceChangedMessage(short value) : ValueChangedMessage<short>(value);
