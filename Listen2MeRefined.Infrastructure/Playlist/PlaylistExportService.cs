using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Playlist.Formats;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Serilog;

namespace Listen2MeRefined.Infrastructure.Playlist;

/// <summary>
/// Default <see cref="IPlaylistExportService"/> implementation.
/// </summary>
public sealed class PlaylistExportService : IPlaylistExportService
{
    private readonly IPlaylistQueueState _queueState;
    private readonly IPlaylistLibraryService _libraryService;
    private readonly IBackgroundTaskStatusService _statusService;
    private readonly ILogger _logger;

    public PlaylistExportService(
        IPlaylistQueueState queueState,
        IPlaylistLibraryService libraryService,
        IBackgroundTaskStatusService statusService,
        ILogger logger)
    {
        _queueState = queueState;
        _libraryService = libraryService;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task ExportAsync(
        PlaylistExportSource source,
        string targetPath,
        IPlaylistFileFormat format,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return;
        }

        var fileName = Path.GetFileName(targetPath);
        var taskHandle = _statusService.StartTask(
            taskKey: "playlist-export",
            displayName: $"Exporting {source.DisplayName}",
            progressKind: TaskProgressKind.Indeterminate);

        IReadOnlyList<AudioModel> songs;
        try
        {
            songs = source.PlaylistId is null
                ? _queueState.DefaultPlaylist.ToList()
                : await _libraryService.GetPlaylistSongsAsync(source.PlaylistId.Value, ct);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load songs for playlist {Playlist}", source.DisplayName);
            _statusService.FailTask(taskHandle, $"Failed to load {source.DisplayName}");
            return;
        }

        try
        {
            await using var stream = File.Create(targetPath);
            await format.WriteAsync(stream, songs, ct);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to write playlist to {Path}", targetPath);
            _statusService.FailTask(taskHandle, $"Failed to write {fileName}");
            return;
        }

        _statusService.CompleteTask(taskHandle, $"Exported {songs.Count} tracks to {fileName}");
    }
}
