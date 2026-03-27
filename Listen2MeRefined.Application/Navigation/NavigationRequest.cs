namespace Listen2MeRefined.Application.Navigation;

/// <summary>
/// Represents a navigation request.
/// </summary>
/// <param name="Route">The route key.</param>
/// <param name="Parameter">An optional navigation parameter.</param>
public sealed record NavigationRequest(string Route, object? Parameter = null);
