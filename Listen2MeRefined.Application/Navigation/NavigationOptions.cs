namespace Listen2MeRefined.Application.Navigation;

/// <summary>
/// Defines runtime defaults for navigation.
/// </summary>
public sealed class NavigationOptions
{
    /// <summary>
    /// Gets or sets the startup route used by the shell.
    /// </summary>
    /// <value>A registered route key. The default is <c>main/home</c>.</value>
    public string DefaultRoute { get; set; } = "main/home";
}
