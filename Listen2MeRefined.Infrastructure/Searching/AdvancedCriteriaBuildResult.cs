using Listen2MeRefined.Infrastructure.Mvvm;

namespace Listen2MeRefined.Infrastructure.Searching;

public sealed record AdvancedCriteriaBuildResult(bool Success, AdvancedSearchCriterion? Criterion, string ErrorMessage);
