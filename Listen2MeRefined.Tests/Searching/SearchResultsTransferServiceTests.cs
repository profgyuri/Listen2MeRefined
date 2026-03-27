using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Searching;

namespace Listen2MeRefined.Tests.Searching;

public class SearchResultsTransferServiceTests
{
    [Fact]
    public void ResolveTransfer_WithSelectedSongsAndMoveMode_ReturnsSelectedForAddAndRemove()
    {
        var service = new SearchResultsTransferService();
        var selected = new AudioModel { Title = "Selected", Path = "selected.mp3" };
        var nonSelected = new AudioModel { Title = "Other", Path = "other.mp3" };

        var decision = service.ResolveTransfer(
            [selected, nonSelected],
            [selected],
            SearchResultsTransferMode.Move);

        Assert.Single(decision.SongsToAdd);
        Assert.Same(selected, decision.SongsToAdd[0]);
        Assert.Single(decision.SongsToRemove);
        Assert.Same(selected, decision.SongsToRemove[0]);
        Assert.True(decision.ClearSelection);
    }

    [Fact]
    public void ResolveTransfer_WithoutSelectionAndCopyMode_ReturnsAllForAddAndNoneForRemove()
    {
        var service = new SearchResultsTransferService();
        var first = new AudioModel { Title = "First", Path = "first.mp3" };
        var second = new AudioModel { Title = "Second", Path = "second.mp3" };

        var decision = service.ResolveTransfer(
            [first, second],
            [],
            SearchResultsTransferMode.Copy);

        Assert.Equal(2, decision.SongsToAdd.Count);
        Assert.Empty(decision.SongsToRemove);
        Assert.True(decision.ClearSelection);
    }
}
