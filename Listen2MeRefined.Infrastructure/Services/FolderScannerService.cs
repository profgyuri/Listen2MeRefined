using Listen2MeRefined.Infrastructure.Storage;
using Listen2MeRefined.Infrastructure.Services.Contracts;
using Listen2MeRefined.Infrastructure.Services.Models;
using Listen2MeRefined.Infrastructure.SystemOperations;
using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Services;

public sealed class FolderScannerService : IFolderScanner
{
    private readonly IFileEnumerator _fileEnumerator;
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IBackgroundTaskStatusService _backgroundTaskStatusService;
    private readonly ILogger _logger;

    public FolderScannerService(
        IFileEnumerator fileEnumerator,
        IFileAnalyzer<AudioModel> audioFileAnalyzer,
        IRepository<AudioModel> audioRepository,
        ISettingsManager<AppSettings> settingsManager,
        IBackgroundTaskStatusService backgroundTaskStatusService,
        ILogger logger)
    {
        _fileEnumerator = fileEnumerator;
        _audioFileAnalyzer = audioFileAnalyzer;
        _audioRepository = audioRepository;
        _settingsManager = settingsManager;
        _backgroundTaskStatusService = backgroundTaskStatusService;
        _logger = logger;
    }

    public async Task ScanAsync(string path)
    {
        await ScanAsync([path]);
    }

    public async Task ScanAsync(IEnumerable<string> paths)
    {
        var pathList = paths
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var taskHandle = _backgroundTaskStatusService.StartTask(
            "folder-scan",
            "Scanning library",
            TaskProgressKind.Determinate);

        try
        {
            foreach (var path in pathList)
            {
                await ScanSinglePathAsync(path, taskHandle);
            }

            _backgroundTaskStatusService.CompleteTask(taskHandle, "Scan completed.");
        }
        catch (Exception ex)
        {
            _backgroundTaskStatusService.FailTask(taskHandle, $"Scan failed: {ex.Message}");
            throw;
        }
    }
    
    public async Task ScanAllAsync()
    {
        var paths =
            _settingsManager.Settings.MusicFolders
                .Select(x => x.FullPath);
        await ScanAsync(paths);
    }

    private async Task ScanSinglePathAsync(string path, TaskHandle taskHandle)
    {
        _logger.Information("[FolderScannerService] Scanning folder for audio files: {Path}", path);

        var files = await _fileEnumerator.EnumerateFilesAsync(path);
        var newSupportedFiles = files
            .Where(IsSupported)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fromDb = (await _audioRepository.ReadAsync())
            .Where(x => !string.IsNullOrWhiteSpace(x.Path)
                        && x.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var existingByPath = fromDb
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .GroupBy(x => x.Path!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var existingToAnalyzePaths = new HashSet<string>(
            newSupportedFiles.Where(existingByPath.ContainsKey),
            StringComparer.OrdinalIgnoreCase);

        newSupportedFiles.ExceptWith(existingToAnalyzePaths);

        var removedFromDisk = fromDb
            .Where(x => string.IsNullOrWhiteSpace(x.Path) || !existingToAnalyzePaths.Contains(x.Path!))
            .ToList();

        var totalUnits = existingToAnalyzePaths.Count + newSupportedFiles.Count;
        var workerHandle = _backgroundTaskStatusService.RegisterWorker(taskHandle, path, totalUnits);

        try
        {
            var toUpdate = new HashSet<AudioModel>();
            foreach (var existingPath in existingToAnalyzePaths)
            {
                var current = existingByPath[existingPath];
                var updated = await _audioFileAnalyzer.AnalyzeAsync(existingPath);
                current.Update(updated);
                toUpdate.Add(current);
                _backgroundTaskStatusService.ReportWorker(workerHandle, 1, totalUnits);
            }

            var newSongs = new List<AudioModel>();
            foreach (var newPath in newSupportedFiles)
            {
                var analyzed = await _audioFileAnalyzer.AnalyzeAsync(newPath);
                newSongs.Add(analyzed);
                _backgroundTaskStatusService.ReportWorker(workerHandle, 1, totalUnits);
            }

            await _audioRepository.UpdateAsync(toUpdate);
            await _audioRepository.SaveAsync(newSongs);
            await _audioRepository.RemoveAsync(removedFromDisk);

            _backgroundTaskStatusService.CompleteWorker(workerHandle);
        }
        catch (Exception ex)
        {
            _backgroundTaskStatusService.FailWorker(
                workerHandle,
                $"Failed scanning '{Path.GetFileName(path)}': {ex.Message}");
            throw;
        }
    }

    private static bool IsSupported(string path)
    {
        return !path.EndsWith(".wav") || new WaveFileReader(path).WaveFormat.Encoding is not WaveFormatEncoding.Extensible;
    }
}
