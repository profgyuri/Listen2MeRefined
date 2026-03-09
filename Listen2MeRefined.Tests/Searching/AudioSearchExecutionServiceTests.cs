using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Listen2MeRefined.Infrastructure.Searching;
using Moq;

namespace Listen2MeRefined.Tests.Searching;

public sealed class AudioSearchExecutionServiceTests
{
    [Fact]
    public async Task ExecuteQuickSearchAsync_EmptyTerm_ReadsAll()
    {
        var repo = new Mock<IRepository<AudioModel>>();
        var advancedReader = new Mock<IAdvancedDataReader<AdvancedFilter, AudioModel>>();
        repo.Setup(x => x.ReadAsync()).ReturnsAsync([new AudioModel { Title = "A", Path = "a" }]);

        var sut = new AudioSearchExecutionService(repo.Object, advancedReader.Object);
        var result = await sut.ExecuteQuickSearchAsync("");

        Assert.Single(result);
        repo.Verify(x => x.ReadAsync(), Times.Once);
        repo.Verify(x => x.ReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAdvancedSearchAsync_AnyMode_UsesMatchAllFalse()
    {
        var repo = new Mock<IRepository<AudioModel>>();
        var advancedReader = new Mock<IAdvancedDataReader<AdvancedFilter, AudioModel>>();
        bool? capturedMatchAll = null;
        advancedReader
            .Setup(x => x.ReadAsync(It.IsAny<IEnumerable<AdvancedFilter>>(), It.IsAny<bool>()))
            .Callback<IEnumerable<AdvancedFilter>, bool>((_, matchAll) => capturedMatchAll = matchAll)
            .ReturnsAsync([]);

        var sut = new AudioSearchExecutionService(repo.Object, advancedReader.Object);
        await sut.ExecuteAdvancedSearchAsync([], SearchMatchMode.Any);

        Assert.False(capturedMatchAll);
    }
}
