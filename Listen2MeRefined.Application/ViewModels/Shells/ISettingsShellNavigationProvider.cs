namespace Listen2MeRefined.Application.ViewModels.Shells;

public interface ISettingsShellNavigationProvider
{
    /// <summary>
    /// Creates navigation items for the setting shell.
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<SettingsShellNavigationItem> CreateNavigationItems();
}
