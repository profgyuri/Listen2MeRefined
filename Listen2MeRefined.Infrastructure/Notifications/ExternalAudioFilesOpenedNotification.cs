using MediatR;

namespace Listen2MeRefined.Infrastructure.Notifications;

public sealed class ExternalAudioFilesOpenedNotification : INotification
{
    public IReadOnlyList<string> Paths { get; }

    public ExternalAudioFilesOpenedNotification(IEnumerable<string> paths)
    {
        Paths = paths
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
