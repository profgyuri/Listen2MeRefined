namespace Listen2MeRefined.Infrastructure.Services;

public class FolderScannerService : IFolderScanner
{
    private readonly IFileEnumerator _fileEnumerator;
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly ILogger _logger;

    public FolderScannerService(
        IFileEnumerator fileEnumerator,
        IFileAnalyzer<AudioModel> audioFileAnalyzer,
        IRepository<AudioModel> audioRepository,
        ILogger logger)
    {
        _fileEnumerator = fileEnumerator;
        _audioFileAnalyzer = audioFileAnalyzer;
        _audioRepository = audioRepository;
        _logger = logger;
    }
    
    #region Implementation of IFolderScanner
    /// <inheritdoc />
    public void Scan(string path)
    {
        _logger.Information("Scanning folder for audio files: {Path}", path);
        var files = _fileEnumerator.EnumerateFiles(path);
        var songs = _audioFileAnalyzer.Analyze(files);
        _audioRepository.Create(songs);
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
        var files = await _fileEnumerator.EnumerateFilesAsync(path);
        var songs = await _audioFileAnalyzer.AnalyzeAsync(files);
        await _audioRepository.CreateAsync(songs);
    }

    /// <inheritdoc />
    public async Task ScanAsync(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            await ScanAsync(path);
        }
    }
    #endregion
}