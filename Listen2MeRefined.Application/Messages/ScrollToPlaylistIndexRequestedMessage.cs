using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class ScrollToPlaylistIndexRequestedMessage(int index) : ValueChangedMessage<int>(index);
