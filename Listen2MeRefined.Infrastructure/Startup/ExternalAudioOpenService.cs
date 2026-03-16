using Listen2MeRefined.Application;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Playback;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Startup;

public sealed class ExternalAudioOpenService : IExternalAudioOpenService
{
    private static readonly HashSet<string> SupportedExtensions = new(
        GlobalConstants.SupportedExtensions,
        StringComparer.OrdinalIgnoreCase);

    private readonly ILogger _logger;
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IPlaylistQueue _playlistQueue;
    private readonly IPlaylistQueueState _queueState;
    private readonly IMusicPlayerController _musicPlayerController;
    private readonly IBackgroundTaskStatusService _backgroundTaskStatusService;
    private readonly IUiDispatcher _ui;

    private string? _currentSongPath;

    public ExternalAudioOpenService(
        ILogger logger,
        IFileAnalyzer<AudioModel> audioFileAnalyzer,
        IPlaylistQueue playlistQueue,
        IPlaylistQueueState queueState,
        IMusicPlayerController musicPlayerController,
        IBackgroundTaskStatusService backgroundTaskStatusService,
        IUiDispatcher ui)
    {
        _logger = logger;
        _audioFileAnalyzer = audioFileAnalyzer;
        _playlistQueue = playlistQueue;
        _queueState = queueState;
        _musicPlayerController = musicPlayerController;
        _backgroundTaskStatusService = backgroundTaskStatusService;
        _ui = ui;
    }

    public async Task OpenAsync(IReadOnlyList<string> candidatePaths, CancellationToken ct = default)
    {
        if (candidatePaths.Count == 0)
        {
            return;
        }

        var normalized = candidatePaths
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
        {
            return;
        }

        Task work = Task.CompletedTask;
        await _ui.InvokeAsync(() =>
        {
            work = OpenOnUiAsync(normalized, ct);
        }, ct);

        await work;
    }

    private async Task OpenOnUiAsync(string[] normalized, CancellationToken ct)
    {
        var insertionBaseIndex = ResolveInsertionBaseIndex();
        var insertIndex = insertionBaseIndex + 1;
        if (_playlistQueue.Count == 0)
        {
            insertIndex = 0;
        }

        var insertedIndices = new List<int>();

        foreach (var candidatePath in normalized)
        {
            ct.ThrowIfCancellationRequested();

            if (!File.Exists(candidatePath))
            {
                _logger.Warning("[ExternalAudioOpenService] Shell-opened file not found: {Path}", candidatePath);
                continue;
            }

            var extension = Path.GetExtension(candidatePath);
            if (!SupportedExtensions.Contains(extension))
            {
                _logger.Information("[ExternalAudioOpenService] Skipping unsupported extension {Extension} for file {Path}", extension, candidatePath);
                ShowQuickStatus($"Unsupported file: {Path.GetFileName(candidatePath)}");
                continue;
            }

            var existingIndex = IndexOfPath(candidatePath);
            if (existingIndex >= 0)
            {
                _logger.Information("[ExternalAudioOpenService] File already exists in playlist, jumping to index {Index}: {Path}", existingIndex, candidatePath);
                await _musicPlayerController.JumpToIndexAsync(existingIndex);
                continue;
            }

            AudioModel analyzed;
            try
            {
                analyzed = await _audioFileAnalyzer.AnalyzeAsync(candidatePath, ct);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "[ExternalAudioOpenService] Failed to analyze file opened from shell: {Path}", candidatePath);
                continue;
            }

            _playlistQueue.Items.Insert(insertIndex, analyzed);
            InsertIntoDefaultPlaylistIfMissing(analyzed, insertIndex);
            insertedIndices.Add(insertIndex);
            insertIndex++;
        }

        if (insertedIndices.Count > 0)
        {
            await _musicPlayerController.JumpToIndexAsync(insertedIndices[0]);
        }
    }

    public void SetCurrentSong(AudioModel? audio)
    {
        _currentSongPath = audio?.Path;
    }

    private int ResolveInsertionBaseIndex()
    {
        if (_playlistQueue.Count == 0)
        {
            return -1;
        }

        if (string.IsNullOrWhiteSpace(_currentSongPath))
        {
            return -1;
        }

        var currentIndex = IndexOfPath(_currentSongPath);
        if (currentIndex >= 0)
        {
            return currentIndex;
        }

        return -1;
    }

    private int IndexOfPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return -1;
        }

        for (var i = 0; i < _playlistQueue.Count; i++)
        {
            if (string.Equals(_playlistQueue[i].Path, path, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void InsertIntoDefaultPlaylistIfMissing(AudioModel song, int preferredInsertIndex)
    {
        if (string.IsNullOrWhiteSpace(song.Path))
        {
            return;
        }

        var existingIndex = IndexOfPath(_queueState.DefaultPlaylist, song.Path);
        if (existingIndex >= 0)
        {
            return;
        }

        if (_queueState.IsDefaultPlaylistActive)
        {
            var defaultInsertIndex = Math.Clamp(preferredInsertIndex, 0, _queueState.DefaultPlaylist.Count);
            _queueState.DefaultPlaylist.Insert(defaultInsertIndex, song);
            return;
        }

        _queueState.DefaultPlaylist.Add(song);
    }

    private static int IndexOfPath(IEnumerable<AudioModel> songs, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return -1;
        }

        var index = 0;
        foreach (var song in songs)
        {
            if (!string.IsNullOrWhiteSpace(song.Path) &&
                song.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    private void ShowQuickStatus(string text)
    {
        var taskHandle = _backgroundTaskStatusService.StartTask("shell-open", text, TaskProgressKind.Indeterminate, priority: 100);
        _backgroundTaskStatusService.CompleteTask(taskHandle, text);
    }
}
