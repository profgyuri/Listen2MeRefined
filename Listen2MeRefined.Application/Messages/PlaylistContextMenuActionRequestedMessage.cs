using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Application.Playlist;

namespace Listen2MeRefined.Application.Messages;

public sealed class PlaylistContextMenuActionRequestedMessage(PlaylistContextMenuActionRequest request)
    : ValueChangedMessage<PlaylistContextMenuActionRequest>(request);
