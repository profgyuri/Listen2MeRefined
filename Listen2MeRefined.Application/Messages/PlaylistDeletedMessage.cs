using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed record PlaylistDeletedMessageData(int PlaylistId);

public sealed class PlaylistDeletedMessage(PlaylistDeletedMessageData value)
    : ValueChangedMessage<PlaylistDeletedMessageData>(value);
