using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public sealed class PinnedFoldersChangedNotification : INotification
{
    public IReadOnlyCollection<string> PinnedFolders { get; }

    public PinnedFoldersChangedNotification(IReadOnlyCollection<string> pinnedFolders)
    {
        PinnedFolders = pinnedFolders;
    }
}
