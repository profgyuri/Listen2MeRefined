using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed record PlaylistSidebarSelectionData(int? PlaylistId);

public sealed class PlaylistSidebarSelectionChangedMessage(PlaylistSidebarSelectionData value)
    : ValueChangedMessage<PlaylistSidebarSelectionData>(value);
