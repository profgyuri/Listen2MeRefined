using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

/// <summary>
/// Requests that the playlist sidebar switch focus to the playlist identified by <paramref name="value"/>.
/// A <see langword="null"/> value means "select the default playlist".
/// </summary>
public sealed class SelectPlaylistRequestedMessage(int? value)
    : ValueChangedMessage<int?>(value);
