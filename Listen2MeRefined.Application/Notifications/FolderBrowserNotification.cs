using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public sealed class FolderBrowserNotification : INotification
{
    public string Path { get; init; }

    public FolderBrowserNotification(string path)
    {
        Path = path;
    }
}