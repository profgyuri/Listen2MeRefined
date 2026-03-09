using System.Globalization;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.ViewModels;

namespace Listen2MeRefined.Infrastructure.Searching;

public sealed class AdvancedSearchCriteriaService : IAdvancedSearchCriteriaService
{
    private static readonly IReadOnlyList<string> NumericRelations = ["Is", "Is not", "Bigger than", "Less than"];
    private static readonly IReadOnlyList<string> TimeRelations = ["Is", "Is not", "More than", "Less than"];
    private static readonly IReadOnlyList<string> StringRelations = ["Is", "Is not", "Contains", "Does not contain"];

    public IReadOnlyList<string> GetColumnNames()
    {
        return typeof(AudioModel)
            .GetProperties()
            .Where(p => p.Name != nameof(AudioModel.Display) && p.Name != nameof(AudioModel.Id))
            .Select(p => p.Name)
            .ToList();
    }

    public SearchRelationDefinition GetRelationDefinition(string columnName)
    {
        return columnName switch
        {
            nameof(AudioModel.Length) => new SearchRelationDefinition(TimeRelations, "mm:ss or sec"),
            nameof(AudioModel.Bitrate) => new SearchRelationDefinition(NumericRelations, "kbps"),
            nameof(AudioModel.BPM) => new SearchRelationDefinition(NumericRelations, "bpm"),
            _ => new SearchRelationDefinition(StringRelations, string.Empty)
        };
    }

    public AdvancedCriteriaBuildResult BuildCriterion(string selectedColumnName, string selectedRelation, string inputText)
    {
        if (string.IsNullOrWhiteSpace(selectedColumnName) ||
            string.IsNullOrWhiteSpace(selectedRelation) ||
            string.IsNullOrWhiteSpace(inputText))
        {
            return new AdvancedCriteriaBuildResult(false, null, "Please select field, relation, and value.");
        }

        if (!TryNormalizeInput(inputText, selectedColumnName, out var normalizedValue, out var normalizationError))
        {
            return new AdvancedCriteriaBuildResult(false, null, normalizationError);
        }

        if (!TryMapOperator(selectedRelation, out var filterOperator))
        {
            return new AdvancedCriteriaBuildResult(false, null, "Selected relation is invalid for this field.");
        }

        var criterion = new AdvancedSearchCriterion(
            selectedColumnName,
            selectedRelation,
            inputText.Trim(),
            normalizedValue,
            filterOperator);
        return new AdvancedCriteriaBuildResult(true, criterion, string.Empty);
    }

    public bool CanBuildCriterion(string selectedColumnName, string selectedRelation, string inputText)
    {
        return BuildCriterion(selectedColumnName, selectedRelation, inputText).Success;
    }

    public IReadOnlyList<AdvancedFilter> BuildFilters(IEnumerable<AdvancedSearchCriterion> criterias)
    {
        return criterias
            .Select(c => new AdvancedFilter(c.Field, c.Operator, c.NormalizedValue))
            .ToList();
    }

    private static bool TryMapOperator(string relation, out AdvancedFilterOperator filterOperator)
    {
        switch (relation)
        {
            case "Is":
                filterOperator = AdvancedFilterOperator.Equal;
                return true;
            case "Is not":
                filterOperator = AdvancedFilterOperator.NotEqual;
                return true;
            case "Contains":
                filterOperator = AdvancedFilterOperator.Contains;
                return true;
            case "Does not contain":
                filterOperator = AdvancedFilterOperator.NotContains;
                return true;
            case "Bigger than":
            case "More than":
                filterOperator = AdvancedFilterOperator.GreaterThan;
                return true;
            case "Less than":
                filterOperator = AdvancedFilterOperator.LessThan;
                return true;
            default:
                filterOperator = default;
                return false;
        }
    }

    private static bool TryNormalizeInput(string inputText, string selectedColumnName, out string normalized, out string error)
    {
        var trimmed = inputText.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            normalized = string.Empty;
            error = "Value cannot be empty.";
            return false;
        }

        if (selectedColumnName is nameof(AudioModel.BPM) or nameof(AudioModel.Bitrate))
        {
            if (!short.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                normalized = string.Empty;
                error = $"{selectedColumnName} must be a whole number.";
                return false;
            }

            normalized = number.ToString(CultureInfo.InvariantCulture);
            error = string.Empty;
            return true;
        }

        if (selectedColumnName == nameof(AudioModel.Length))
        {
            if (!TryParseLength(trimmed, out var length))
            {
                normalized = string.Empty;
                error = "Length must be mm:ss or total seconds.";
                return false;
            }

            normalized = length.ToString("c", CultureInfo.InvariantCulture);
            error = string.Empty;
            return true;
        }

        normalized = trimmed;
        error = string.Empty;
        return true;
    }

    private static bool TryParseLength(string value, out TimeSpan length)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
        {
            length = TimeSpan.FromSeconds(seconds);
            return true;
        }

        if (TimeSpan.TryParseExact(
                value,
                ["m\\:ss", "mm\\:ss", "h\\:mm\\:ss", "hh\\:mm\\:ss", "c"],
                CultureInfo.InvariantCulture,
                out var parsed) &&
            parsed >= TimeSpan.Zero)
        {
            length = parsed;
            return true;
        }

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out parsed) && parsed >= TimeSpan.Zero)
        {
            length = parsed;
            return true;
        }

        length = TimeSpan.Zero;
        return false;
    }
}
