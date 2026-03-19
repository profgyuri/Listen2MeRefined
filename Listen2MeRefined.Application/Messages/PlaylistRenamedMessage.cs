using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed record PlaylistRenamedMessageData(int PlaylistId, string Name);

public sealed class PlaylistRenamedMessage(PlaylistRenamedMessageData value)
    : ValueChangedMessage<PlaylistRenamedMessageData>(value);
