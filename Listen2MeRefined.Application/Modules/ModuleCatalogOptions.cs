namespace Listen2MeRefined.Application.Modules;

/// <summary>
/// Defines discovery behavior for module loading.
/// </summary>
public sealed class ModuleCatalogOptions
{
    /// <summary>
    /// Gets or sets a value that indicates whether assembly scanning is enabled.
    /// </summary>
    /// <value><see langword="true"/> to scan assemblies; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</value>
    public bool EnableAssemblyScan { get; set; } = true;

    /// <summary>
    /// Gets or sets assembly name prefixes considered during scanning.
    /// </summary>
    /// <value>An array of prefixes. The default is <c>UtilityCollection.</c>.</value>
    public string[] ScanAssemblyPrefixes { get; set; } = ["Listen2MeRefined."];
}
