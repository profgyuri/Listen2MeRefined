using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class ActivateViewedPlaylistMessage() : ValueChangedMessage<bool>(true);
