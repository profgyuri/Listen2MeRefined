using Listen2MeRefined.Application;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Infrastructure.Playlist;

/// <summary>
/// Imports dropped files into default and active playlist collections.
/// </summary>
public sealed class ExternalDropImportService : IExternalDropImportService
{
    private static readonly HashSet<string> SupportedExtensions = new(
        GlobalConstants.SupportedExtensions,
        StringComparer.OrdinalIgnoreCase);

    private readonly IPlaylistQueueState _queueState;
    private readonly IFileScanner _fileScanner;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IDroppedSongFolderPromptService _droppedSongFolderPromptService;

    public ExternalDropImportService(
        IPlaylistQueueState queueState,
        IFileScanner fileScanner,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IDroppedSongFolderPromptService droppedSongFolderPromptService)
    {
        _queueState = queueState;
        _fileScanner = fileScanner;
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _droppedSongFolderPromptService = droppedSongFolderPromptService;
    }

    /// <inheritdoc />
    public async Task HandleExternalFileDropAsync(IReadOnlyList<string> droppedPaths, int insertIndex, CancellationToken ct = default)
    {
        var supportedFiles = droppedPaths
            .Where(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x))
            .Where(x => SupportedExtensions.Contains(Path.GetExtension(x)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (supportedFiles.Count == 0)
        {
            return;
        }

        var folders = supportedFiles
            .Select(Path.GetDirectoryName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        await PromptAndPersistMissingMusicFoldersAsync(folders, ct);

        var scannedSongs = new List<AudioModel>(supportedFiles.Count);
        foreach (var file in supportedFiles)
        {
            scannedSongs.Add(await _fileScanner.ScanAsync(file, ct));
        }

        var defaultTargetIndex = Math.Clamp(insertIndex, 0, _queueState.DefaultPlaylist.Count);
        foreach (var song in scannedSongs)
        {
            _queueState.DefaultPlaylist.Insert(defaultTargetIndex, song);
            defaultTargetIndex++;
        }

        if (!_queueState.IsDefaultPlaylistActive)
        {
            return;
        }

        var playListTargetIndex = Math.Clamp(insertIndex, 0, _queueState.PlayList.Count);
        foreach (var song in scannedSongs)
        {
            _queueState.PlayList.Insert(playListTargetIndex, song);
            playListTargetIndex++;
        }
    }

    /// <summary>
    /// Prompts for folders discovered via drag-and-drop and persists accepted or muted decisions to settings.
    /// </summary>
    /// <param name="folders">The candidate folders discovered from dropped files.</param>
    /// <param name="ct">A token that can cancel the prompt flow.</param>
    private async Task PromptAndPersistMissingMusicFoldersAsync(IEnumerable<string> folders, CancellationToken ct)
    {
        var existing = _settingsReader.GetMusicFolders();
        var toAdd = existing.ToList();
        var mutedFolders = _settingsReader.GetMutedDroppedSongFolders().ToList();
        var changed = false;
        var mutedChanged = false;

        foreach (var folder in folders)
        {
            if (existing.Contains(folder, StringComparer.OrdinalIgnoreCase) ||
                toAdd.Contains(folder, StringComparer.OrdinalIgnoreCase) ||
                mutedFolders.Contains(folder, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var decision = await _droppedSongFolderPromptService.PromptAsync(folder, ct);
            if (decision == AddDroppedSongFolderDecision.AddFolder)
            {
                toAdd.Add(folder);
                changed = true;
            }
            else if (decision == AddDroppedSongFolderDecision.SkipAndDontAskAgain)
            {
                mutedFolders.Add(folder);
                mutedChanged = true;
            }
        }

        if (changed)
        {
            _settingsWriter.SetMusicFolders(toAdd);
        }

        if (mutedChanged)
        {
            _settingsWriter.SetMutedDroppedSongFolders(mutedFolders);
        }
    }
}
