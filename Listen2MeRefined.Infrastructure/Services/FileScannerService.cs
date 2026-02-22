using Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.SystemOperations;

namespace Listen2MeRefined.Infrastructure.Services;

public sealed class FileScannerService : IFileScanner
{
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IAudioRepository _audioRepository;

    public FileScannerService(IFileAnalyzer<AudioModel> audioFileAnalyzer, IAudioRepository audioRepository)
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
