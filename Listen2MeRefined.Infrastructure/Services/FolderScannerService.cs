using Listen2MeRefined.Infrastructure.Data;
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
    public void Scan(string path)
    {
        _logger.Information("Scanning folder for audio files: {Path}", path);
        var files = _fileEnumerator.EnumerateFiles(path).ToHashSet();
        var fromDb = _audioRepository.Read().ToList();

        for (var i = 0; i < fromDb.Count; i++)
        {
            var current = fromDb[i];

            if (!files.Contains(current.Path!))
            {
                continue;
            }

            files.Remove(current.Path!);
            fromDb.RemoveAt(i);
            i--;
            
            var updated = _audioFileAnalyzer.Analyze(current.Path!);
            current.Update(updated);
            
            _audioRepository.UpdateAsync(current);
        }
        
        var newSongs =  _audioFileAnalyzer.Analyze(files);
        _audioRepository.Create(newSongs);
        _audioRepository.Delete(fromDb);
    }

    /// <inheritdoc />
    public void Scan(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            Scan(path);
        }
    }

    /// <inheritdoc />
    public async Task ScanAsync(string path)
    {
        _logger.Information("Scanning folder for audio files: {Path}", path);
        var files = (await _fileEnumerator.EnumerateFilesAsync(path)).ToHashSet();
        var fromDb = (await _audioRepository.ReadAsync()).ToList();

        for (var i = 0; i < fromDb.Count; i++)
        {
            var current = fromDb[i];

            if (!files.Contains(current.Path!))
            {
                continue;
            }

            files.Remove(current.Path!);
            fromDb.RemoveAt(i);
            i--;
            
            var updated = await _audioFileAnalyzer.AnalyzeAsync(current.Path!);
            current.Update(updated);
            
            await _audioRepository.UpdateAsync(current);
        }
        
        var newSongs = await _audioFileAnalyzer.AnalyzeAsync(files);
        await _audioRepository.CreateAsync(newSongs);
        await _audioRepository.DeleteAsync(fromDb);
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
    public void ScanAll()
    {
        var paths =
            _settingsManager.Settings.MusicFolders
                .Select(x => x.FullPath);
        Scan(paths);
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
}