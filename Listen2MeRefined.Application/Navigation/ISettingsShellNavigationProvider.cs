using Listen2MeRefined.Application.ViewModels.Shells;

namespace Listen2MeRefined.Application.Navigation;

public interface ISettingsShellNavigationProvider
{
    /// <summary>
    /// Creates navigation items for the setting shell.
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<SettingsShellNavigationItem> CreateNavigationItems();
}
