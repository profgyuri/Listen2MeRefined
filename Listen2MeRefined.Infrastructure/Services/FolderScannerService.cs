namespace Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Storage;
using Listen2MeRefined.Infrastructure.SystemOperations;
using NAudio.Wave;

public sealed class FolderScannerService : IFolderScanner
{
    private readonly IFileEnumerator _fileEnumerator;
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly ILogger _logger;

    public FolderScannerService(
        IFileEnumerator fileEnumerator,
        IFileAnalyzer<AudioModel> audioFileAnalyzer,
        IRepository<AudioModel> audioRepository,
        ISettingsManager<AppSettings> settingsManager,
        ILogger logger)
    {
        _fileEnumerator = fileEnumerator;
        _audioFileAnalyzer = audioFileAnalyzer;
        _audioRepository = audioRepository;
        _settingsManager = settingsManager;
        _logger = logger;
    }
    
    #region Implementation of IFolderScanner
    /// <inheritdoc />
    public async Task ScanAsync(string path, CancellationToken ct)
    {
        _logger.Information("Scanning folder for audio files: {Path}", path);
        var files = await _fileEnumerator.EnumerateFilesAsync(path);
        var newSupportedFiles = files
            .Where(IsSupported)
            .ToHashSet();
        var fromDb = (await _audioRepository.ReadAsync())
            .Where(x => x.Path.StartsWith(path))
            .ToList();
        var toUpdate = new HashSet<AudioModel>();

        for (var i = 0; i < fromDb.Count; i++)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.Information("Scanning folder cancelled: {Path}", path);
                return;
            }

            var current = fromDb[i];

            if (!newSupportedFiles.Contains(current.Path!))
            {
                continue;
            }

            newSupportedFiles.Remove(current.Path!);
            fromDb.RemoveAt(i);
            i--;

            var updated = await _audioFileAnalyzer.AnalyzeAsync(current.Path!);
            current.Update(updated);

            toUpdate.Add(current);
        }
        
        var newSongs = await _audioFileAnalyzer.AnalyzeAsync(newSupportedFiles);
        await _audioRepository.UpdateAsync(toUpdate);
        await _audioRepository.SaveAsync(newSongs);
        await _audioRepository.RemoveAsync(fromDb);
    }

    /// <inheritdoc />
    public async Task ScanAsync(IEnumerable<string> paths, CancellationToken ct)
    {
        foreach (var path in paths)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            await ScanAsync(path, ct);
        }
    }
    
    /// <inheritdoc />
    public async Task ScanAllAsync(CancellationToken ct)
    {
        var paths =
            _settingsManager.Settings.MusicFolders
                .Select(x => x.FullPath);
        await ScanAsync(paths, ct);
    }
    #endregion

    private static bool IsSupported(string path)
    {
        return !path.EndsWith(".wav") || new WaveFileReader(path).WaveFormat.Encoding is not WaveFormatEncoding.Extensible;
    }
}