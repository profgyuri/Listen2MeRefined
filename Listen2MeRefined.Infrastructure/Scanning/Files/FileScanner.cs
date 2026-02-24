namespace Listen2MeRefined.Infrastructure.Scanning.Files;

public sealed class FileScanner : IFileScanner
{
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IAudioRepository _audioRepository;

    public FileScanner(IFileAnalyzer<AudioModel> audioFileAnalyzer, IAudioRepository audioRepository)
    {
        _audioFileAnalyzer = audioFileAnalyzer;
        _audioRepository = audioRepository;
    }

    public async Task<AudioModel> ScanAsync(string path, CancellationToken ct = default)
    {
        var existing = await _audioRepository.ReadByPathAsync(path);
        var updated = await _audioFileAnalyzer.AnalyzeAsync(path, ct);

        if (existing is null)
        {
            await _audioRepository.SaveAsync(updated);
            return updated;
        }

        existing.Update(updated);
        await _audioRepository.UpdateAsync(existing);
        return existing;
    }
}
