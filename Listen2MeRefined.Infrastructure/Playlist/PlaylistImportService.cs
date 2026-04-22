using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Playlist.Formats;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Serilog;

namespace Listen2MeRefined.Infrastructure.Playlist;

/// <summary>
/// Default <see cref="IPlaylistImportService"/> implementation.
/// </summary>
public sealed class PlaylistImportService : IPlaylistImportService
{
    private readonly IPlaylistFormatRegistry _registry;
    private readonly IPlaylistQueueState _queueState;
    private readonly IAudioRepository _audioRepository;
    private readonly IFileScanner _fileScanner;
    private readonly IReplaceDefaultPlaylistPrompt _prompt;
    private readonly IBackgroundTaskStatusService _statusService;
    private readonly IMessenger _messenger;
    private readonly ILogger _logger;

    public PlaylistImportService(
        IPlaylistFormatRegistry registry,
        IPlaylistQueueState queueState,
        IAudioRepository audioRepository,
        IFileScanner fileScanner,
        IReplaceDefaultPlaylistPrompt prompt,
        IBackgroundTaskStatusService statusService,
        IMessenger messenger,
        ILogger logger)
    {
        _registry = registry;
        _queueState = queueState;
        _audioRepository = audioRepository;
        _fileScanner = fileScanner;
        _prompt = prompt;
        _statusService = statusService;
        _messenger = messenger;
        _logger = logger;
    }

    public async Task ImportAsync(string playlistFilePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(playlistFilePath) || !File.Exists(playlistFilePath))
        {
            _logger.Warning("Playlist import cancelled — file not found: {Path}", playlistFilePath);
            return;
        }

        var format = _registry.ResolveForPath(playlistFilePath);
        if (format is null)
        {
            _logger.Warning("Unsupported playlist format for {Path}", playlistFilePath);
            return;
        }

        var fileName = Path.GetFileName(playlistFilePath);
        var taskHandle = _statusService.StartTask(
            taskKey: "playlist-import",
            displayName: $"Importing playlist {fileName}",
            progressKind: TaskProgressKind.Indeterminate);

        IReadOnlyList<PlaylistFileEntry> entries;
        try
        {
            await using var stream = File.OpenRead(playlistFilePath);
            entries = await format.ReadAsync(stream, playlistFilePath, ct);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to read playlist file {Path}", playlistFilePath);
            _statusService.FailTask(taskHandle, $"Failed to read {fileName}");
            return;
        }

        var resolved = new List<AudioModel>(entries.Count);
        var missingCount = 0;

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(entry.Path))
            {
                missingCount++;
                continue;
            }

            try
            {
                var existing = await _audioRepository.ReadByPathAsync(entry.Path);
                if (existing is not null)
                {
                    resolved.Add(existing);
                    continue;
                }

                if (!File.Exists(entry.Path))
                {
                    _logger.Information("Playlist entry missing on disk: {Path}", entry.Path);
                    missingCount++;
                    continue;
                }

                var scanned = await _fileScanner.ScanAsync(entry.Path, ct);
                resolved.Add(scanned);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to resolve playlist entry {Path}", entry.Path);
                missingCount++;
            }
        }

        if (resolved.Count == 0)
        {
            _statusService.FailTask(taskHandle, $"No importable tracks in {fileName}");
            return;
        }

        var existingCount = _queueState.DefaultPlaylist.Count;
        if (existingCount > 0)
        {
            var confirmed = await _prompt.ConfirmReplaceAsync(existingCount, resolved.Count, ct);
            if (!confirmed)
            {
                _statusService.CompleteTask(taskHandle, "Import cancelled");
                return;
            }
        }

        _queueState.DefaultPlaylist.Clear();
        foreach (var track in resolved)
        {
            _queueState.DefaultPlaylist.Add(track);
        }

        _messenger.Send(new SelectPlaylistRequestedMessage(null));

        var summary = missingCount > 0
            ? $"Imported {resolved.Count} tracks ({missingCount} missing)"
            : $"Imported {resolved.Count} tracks";
        _statusService.CompleteTask(taskHandle, summary);
    }
}
