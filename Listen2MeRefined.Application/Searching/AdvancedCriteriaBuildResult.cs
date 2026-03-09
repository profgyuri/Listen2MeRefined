namespace Listen2MeRefined.Application.Searching;

public sealed record AdvancedCriteriaBuildResult(bool Success, AdvancedSearchCriterion? Criterion, string ErrorMessage);
