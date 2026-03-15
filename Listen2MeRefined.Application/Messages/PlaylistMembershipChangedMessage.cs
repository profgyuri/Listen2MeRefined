using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed record PlaylistMembershipChangedMessageData(int PlaylistId);

public sealed class PlaylistMembershipChangedMessage(PlaylistMembershipChangedMessageData value)
    : ValueChangedMessage<PlaylistMembershipChangedMessageData>(value);
