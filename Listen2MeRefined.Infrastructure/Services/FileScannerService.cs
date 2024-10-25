namespace Listen2MeRefined.Infrastructure.Services;

using Listen2MeRefined.Infrastructure.SystemOperations;

public class FileScannerService : IFileScanner
{
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IRepository<AudioModel> _audioRepository;

    public FileScannerService(IFileAnalyzer<AudioModel> audioFileAnalyzer, IRepository<AudioModel> audioRepository)
    {
        _audioFileAnalyzer = audioFileAnalyzer;
        _audioRepository = audioRepository;
    }

    public async Task<AudioModel> ScanAsync(string path)
    {
        var existing = (await _audioRepository.ReadAsync())
            .FirstOrDefault(x => x.Path == path);
        var updated = await _audioFileAnalyzer.AnalyzeAsync(path);

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