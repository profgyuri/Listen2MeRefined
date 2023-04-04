using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.SystemOperations;
using NAudio.Wave;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Services;

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
    public async Task ScanAsync(string path)
    {
        _logger.Information("Scanning folder for audio files: {Path}", path);
        var files = await _fileEnumerator.EnumerateFilesAsync(path);
        var filteredFiles =
            files.Where(IsSupported).ToHashSet();
        var fromDb = 
            (await _audioRepository.ReadAsync())
            .Where(x => x.Path.StartsWith(path))
            .ToList();
        var toUpdate = new HashSet<AudioModel>();

        for (var i = 0; i < fromDb.Count; i++)
        {
            var current = fromDb[i];

            if (!filteredFiles.Contains(current.Path!))
            {
                continue;
            }

            filteredFiles.Remove(current.Path!);
            fromDb.RemoveAt(i);
            i--;

            var updated = await _audioFileAnalyzer.AnalyzeAsync(current.Path!);
            current.Update(updated);

            toUpdate.Add(current);
        }
        
        var newSongs = await _audioFileAnalyzer.AnalyzeAsync(filteredFiles);
        await Task.Run(() => _audioRepository.UpdateAsync(toUpdate));
        await Task.Run(() => _audioRepository.SaveAsync(newSongs));
        await Task.Run(() => _audioRepository.RemoveAsync(fromDb));
    }

    /// <inheritdoc />
    public async Task ScanAsync(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            await ScanAsync(path);
        }
    }
    
    /// <inheritdoc />
    public async Task ScanAllAsync()
    {
        var paths =
            _settingsManager.Settings.MusicFolders
                .Select(x => x.FullPath);
        await ScanAsync(paths);
    }
    #endregion

    private static bool IsSupported(string path)
    {
        return !path.EndsWith(".wav") || new WaveFileReader(path).WaveFormat.Encoding is WaveFormatEncoding.Extensible;
    }
}