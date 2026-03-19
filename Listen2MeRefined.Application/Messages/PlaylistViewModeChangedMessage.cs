using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class PlaylistViewModeChangedMessage(bool useCompactPlaylistView)
    : ValueChangedMessage<bool>(useCompactPlaylistView);
