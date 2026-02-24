using System.Collections.Concurrent;
using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Scanning.Files;
using Listen2MeRefined.Infrastructure.Services.Models;
using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Scanning.Folders;

public sealed class FolderScanner : IFolderScanner
{
    private readonly IFileEnumerator _fileEnumerator;
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IAudioRepository _audioRepository;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IBackgroundTaskStatusService _backgroundTaskStatusService;
    private readonly ILogger _logger;
    private readonly int _maxWorkers;

    public FolderScanner(
        IFileEnumerator fileEnumerator,
        IFileAnalyzer<AudioModel> audioFileAnalyzer,
        IAudioRepository audioRepository,
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
        _maxWorkers = Math.Max(1, Math.Min(Environment.ProcessorCount - 1, 8));
    }

    public Task ScanAsync(string path, ScanMode mode = ScanMode.Incremental, CancellationToken ct = default)
    {
        return ScanAsync([new FolderScanRequest(path, false)], mode, ct);
    }

    public async Task ScanAsync(
        IEnumerable<FolderScanRequest> requests,
        ScanMode mode = ScanMode.Incremental,
        CancellationToken ct = default)
    {
        var requestList = NormalizeRequests(requests);
        var taskHandle = _backgroundTaskStatusService.StartTask(
            "folder-scan",
            "Scanning library",
            TaskProgressKind.Determinate);

        using var analyzerLimiter = new SemaphoreSlim(_maxWorkers, _maxWorkers);
        using var repositoryLimiter = new SemaphoreSlim(1, 1);
        try
        {
            var folderTasks = requestList
                .Select(x => ScanSinglePathAsync(x, mode, taskHandle, analyzerLimiter, repositoryLimiter, ct))
                .ToArray();

            var folderResults = await Task.WhenAll(folderTasks);
            var totalAdded = folderResults.Sum(x => x.Added);
            var totalUpdated = folderResults.Sum(x => x.Updated);
            var totalRemoved = folderResults.Sum(x => x.Removed);
            var totalSkipped = folderResults.Sum(x => x.Skipped);
            var totalFailed = folderResults.Sum(x => x.Failed);

            var summary = string.Join(
                Environment.NewLine,
                "Scan completed.",
                $"Added: {totalAdded}",
                $"Updated: {totalUpdated}",
                $"Removed: {totalRemoved}",
                $"Skipped: {totalSkipped}",
                $"Failed: {totalFailed}");
            _backgroundTaskStatusService.CompleteTask(taskHandle, summary);
            _logger.Information("[FolderScannerService] {Summary}", summary);
        }
        catch (OperationCanceledException)
        {
            const string canceled = "Scan canceled.";
            _backgroundTaskStatusService.FailTask(taskHandle, canceled);
            _logger.Warning("[FolderScannerService] {Message}", canceled);
            throw;
        }
        catch (Exception ex)
        {
            _backgroundTaskStatusService.FailTask(taskHandle, $"Scan failed: {ex.Message}");
            throw;
        }
    }

    public async Task ScanAllAsync(ScanMode mode = ScanMode.Incremental, CancellationToken ct = default)
    {
        var requests = _settingsManager.Settings.MusicFolders
            .Select(x => new FolderScanRequest(x.FullPath, x.IncludeSubdirectories));
        await ScanAsync(requests, mode, ct);
    }

    private async Task<FolderScanResult> ScanSinglePathAsync(
        FolderScanRequest request,
        ScanMode mode,
        TaskHandle taskHandle,
        SemaphoreSlim analyzerLimiter,
        SemaphoreSlim repositoryLimiter,
        CancellationToken ct)
    {
        var path = request.Path;
        _logger.Information(
            "[FolderScannerService] Scanning folder {Path} (recursive: {Recursive}, mode: {Mode})",
            path,
            request.IncludeSubdirectories,
            mode);

        var files = new List<string>();
        await foreach (var file in _fileEnumerator.EnumerateFilesAsync(path, request.IncludeSubdirectories, ct))
        {
            files.Add(file);
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        var discoveredFileSet = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<AudioModel> fromDb;
        await repositoryLimiter.WaitAsync(ct);
        try
        {
            fromDb = await _audioRepository.ReadByFolderScopeAsync(path, request.IncludeSubdirectories);
        }
        finally
        {
            repositoryLimiter.Release();
        }
        var existingByPath = fromDb
            .Where(x => !string.IsNullOrWhiteSpace(x.Path))
            .GroupBy(x => x.Path!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var removedFromDisk = fromDb
            .Where(x => string.IsNullOrWhiteSpace(x.Path) || !discoveredFileSet.Contains(x.Path!))
            .ToList();

        var workerHandle = _backgroundTaskStatusService.RegisterWorker(taskHandle, path, files.Count);
        var toUpdate = new ConcurrentBag<AudioModel>();
        var newSongs = new ConcurrentBag<AudioModel>();
        var failedPaths = new ConcurrentQueue<string>();
        var analyzed = 0;
        var skipped = 0;
        var failed = 0;

        try
        {
            var processTasks = files
                .Select(filePath => ProcessFileAsync(
                    filePath,
                    mode,
                    existingByPath,
                    toUpdate,
                    newSongs,
                    failedPaths,
                    workerHandle,
                    files.Count,
                    analyzerLimiter,
                    onAnalyzed: () => Interlocked.Increment(ref analyzed),
                    onSkipped: () => Interlocked.Increment(ref skipped),
                    onFailed: () => Interlocked.Increment(ref failed),
                    ct))
                .ToArray();

            await Task.WhenAll(processTasks);

            var updates = toUpdate.ToArray();
            var inserts = newSongs.ToArray();

            await repositoryLimiter.WaitAsync(ct);
            try
            {
                _logger.Debug(
                    "[FolderScannerService] Persisting folder {Path}: insert {InsertCount}, update {UpdateCount}, remove {RemoveCount}",
                    path,
                    inserts.Length,
                    updates.Length,
                    removedFromDisk.Count);
                await _audioRepository.PersistScanChangesAsync(inserts, updates, removedFromDisk);
            }
            finally
            {
                repositoryLimiter.Release();
            }

            _backgroundTaskStatusService.CompleteWorker(workerHandle);

            if (!failedPaths.IsEmpty)
            {
                _logger.Warning(
                    "[FolderScannerService] Folder {Path} had {Failed} file analysis failures.",
                    path,
                    failed);
            }

            return new FolderScanResult(
                inserts.Length,
                updates.Length,
                removedFromDisk.Count,
                skipped,
                failed);
        }
        catch (OperationCanceledException)
        {
            _backgroundTaskStatusService.FailWorker(workerHandle, $"Scan canceled for '{Path.GetFileName(path)}'.");
            throw;
        }
        catch (Exception ex)
        {
            _backgroundTaskStatusService.FailWorker(
                workerHandle,
                $"Failed scanning '{Path.GetFileName(path)}': {ex.Message}");
            _logger.Error(ex, "[FolderScannerService] Failed to scan folder {Path}", path);
            return new FolderScanResult(0, 0, 0, skipped, failed + Math.Max(1, files.Count - analyzed - skipped));
        }
    }

    private async Task ProcessFileAsync(
        string filePath,
        ScanMode mode,
        IReadOnlyDictionary<string, AudioModel> existingByPath,
        ConcurrentBag<AudioModel> toUpdate,
        ConcurrentBag<AudioModel> newSongs,
        ConcurrentQueue<string> failedPaths,
        WorkerHandle workerHandle,
        int totalUnits,
        SemaphoreSlim analyzerLimiter,
        Action onAnalyzed,
        Action onSkipped,
        Action onFailed,
        CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            if (IsUnsupportedForNow(filePath))
            {
                onSkipped();
                return;
            }

            var hasExisting = existingByPath.TryGetValue(filePath, out var existing);
            var shouldAnalyze = mode == ScanMode.FullRefresh
                                || !hasExisting
                                || HasFileChanged(filePath, existing!);
            if (!shouldAnalyze)
            {
                onSkipped();
                return;
            }

            await analyzerLimiter.WaitAsync(ct);
            try
            {
                var analyzed = await _audioFileAnalyzer.AnalyzeAsync(filePath, ct);
                if (hasExisting)
                {
                    existing!.Update(analyzed);
                    toUpdate.Add(existing);
                }
                else
                {
                    newSongs.Add(analyzed);
                }
            }
            finally
            {
                analyzerLimiter.Release();
            }

            onAnalyzed();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            failedPaths.Enqueue(filePath);
            _logger.Error(ex, "[FolderScannerService] Failed to analyze {Path}", filePath);
            onFailed();
        }
        finally
        {
            _backgroundTaskStatusService.ReportWorker(workerHandle, 1, totalUnits);
        }
    }

    private static bool HasFileChanged(string path, AudioModel existing)
    {
        var info = new FileInfo(path);
        return existing.LastWriteUtc != info.LastWriteTimeUtc || existing.LengthBytes != info.Length;
    }

    private bool IsUnsupportedForNow(string path)
    {
        if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var reader = new WaveFileReader(path);
            return reader.WaveFormat.Encoding == WaveFormatEncoding.Extensible;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "[FolderScannerService] Could not pre-check wav format for {Path}", path);
            return false;
        }
    }

    private static IReadOnlyList<FolderScanRequest> NormalizeRequests(IEnumerable<FolderScanRequest> requests)
    {
        var byPath = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var request in requests)
        {
            if (string.IsNullOrWhiteSpace(request.Path))
            {
                continue;
            }

            var normalizedPath = request.Path.Trim();
            if (byPath.TryGetValue(normalizedPath, out var current))
            {
                byPath[normalizedPath] = current || request.IncludeSubdirectories;
            }
            else
            {
                byPath[normalizedPath] = request.IncludeSubdirectories;
            }
        }

        return byPath
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new FolderScanRequest(x.Key, x.Value))
            .ToArray();
    }

    private readonly record struct FolderScanResult(
        int Added,
        int Updated,
        int Removed,
        int Skipped,
        int Failed);
}
