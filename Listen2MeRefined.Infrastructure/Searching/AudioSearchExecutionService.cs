using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;

namespace Listen2MeRefined.Infrastructure.Searching;

public sealed class AudioSearchExecutionService : IAudioSearchExecutionService
{
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IAdvancedDataReader<AdvancedFilter, AudioModel> _advancedDataReader;

    public AudioSearchExecutionService(
        IRepository<AudioModel> audioRepository,
        IAdvancedDataReader<AdvancedFilter, AudioModel> advancedDataReader)
    {
        _audioRepository = audioRepository;
        _advancedDataReader = advancedDataReader;
    }

    public async Task<IReadOnlyList<AudioModel>> ExecuteQuickSearchAsync(string searchTerm)
    {
        var results = string.IsNullOrWhiteSpace(searchTerm)
            ? await _audioRepository.ReadAsync()
            : await _audioRepository.ReadAsync(searchTerm);
        return results?.ToArray() ?? [];
    }

    public async Task<IReadOnlyList<AudioModel>> ExecuteAdvancedSearchAsync(IEnumerable<AdvancedFilter> filters, SearchMatchMode matchMode)
    {
        var matchAll = matchMode == SearchMatchMode.All;
        return (await _advancedDataReader.ReadAsync(filters, matchAll)).ToArray();
    }
}
