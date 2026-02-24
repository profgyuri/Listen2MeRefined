using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Searching;
using Listen2MeRefined.Infrastructure.ViewModels;

namespace Listen2MeRefined.Tests.Searching;

public sealed class AdvancedSearchCriteriaServiceTests
{
    private readonly AdvancedSearchCriteriaService _sut = new();

    [Fact]
    public void GetRelationDefinition_Length_ReturnsTimeRelationsAndSuffix()
    {
        var result = _sut.GetRelationDefinition(nameof(AudioModel.Length));

        Assert.Contains("More than", result.Relations);
        Assert.Equal("mm:ss or sec", result.RangeSuffixText);
    }

    [Fact]
    public void BuildCriterion_InvalidNumericInput_FailsWithMessage()
    {
        var result = _sut.BuildCriterion(nameof(AudioModel.BPM), "Bigger than", "abc");

        Assert.False(result.Success);
        Assert.Null(result.Criterion);
        Assert.Contains("whole number", result.ErrorMessage);
    }

    [Fact]
    public void BuildCriterion_LengthInput_NormalizesToConstantTimeFormat()
    {
        var result = _sut.BuildCriterion(nameof(AudioModel.Length), "Is", "03:05");

        Assert.True(result.Success);
        Assert.NotNull(result.Criterion);
        Assert.Equal("00:03:05", result.Criterion!.NormalizedValue);
        Assert.Equal("03:05", result.Criterion.RawValue);
        Assert.Equal(AdvancedFilterOperator.Equal, result.Criterion.Operator);
    }

    [Fact]
    public void BuildFilters_MapsCriteriaToAdvancedFilters()
    {
        var criteria = new[]
        {
            new AdvancedSearchCriterion(
                nameof(AudioModel.Title),
                "Contains",
                "rock",
                "rock",
                AdvancedFilterOperator.Contains)
        };

        var filters = _sut.BuildFilters(criteria);

        var filter = Assert.Single(filters);
        Assert.Equal(nameof(AudioModel.Title), filter.Field);
        Assert.Equal(AdvancedFilterOperator.Contains, filter.Operator);
        Assert.Equal("rock", filter.Value);
    }
}

