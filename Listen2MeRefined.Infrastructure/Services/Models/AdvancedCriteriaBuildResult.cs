using Listen2MeRefined.Infrastructure.Mvvm;

namespace Listen2MeRefined.Infrastructure.Services.Models;

public sealed record AdvancedCriteriaBuildResult(bool Success, AdvancedSearchCriterion? Criterion, string ErrorMessage);
