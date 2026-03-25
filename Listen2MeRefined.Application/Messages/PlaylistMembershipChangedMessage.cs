using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class PlaylistMembershipChangedMessage(int playlistId)
    : ValueChangedMessage<int>(playlistId);
