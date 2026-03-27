using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed record PlaylistCreatedMessageData(int PlaylistId, string Name);

public sealed class PlaylistCreatedMessage(PlaylistCreatedMessageData value)
    : ValueChangedMessage<PlaylistCreatedMessageData>(value);
