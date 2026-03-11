namespace Listen2MeRefined.Application.Navigation;

/// <summary>
/// Represents a resolved navigation target.
/// </summary>
/// <param name="Route">The route key.</param>
/// <param name="ViewModelType">The target view model type.</param>
public sealed record NavigationTarget(string Route, Type ViewModelType);