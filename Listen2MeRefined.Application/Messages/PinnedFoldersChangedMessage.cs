using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class PinnedFoldersChangedMessage(IReadOnlyCollection<string> pinnedFolders)
    : ValueChangedMessage<IReadOnlyCollection<string>>(pinnedFolders);
