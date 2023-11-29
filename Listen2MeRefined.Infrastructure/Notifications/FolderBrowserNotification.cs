namespace Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

public sealed class FolderBrowserNotification : INotification
{
    public string Path { get; init; }

    public FolderBrowserNotification(string path)
    {
        Path = path;
    }
}