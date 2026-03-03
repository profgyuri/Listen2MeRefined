namespace Listen2MeRefined.Infrastructure.Settings;

public interface IAppThemeService
{
    IReadOnlyList<string> GetThemeModes();
    IReadOnlyList<string> GetAccentColors();
    void ApplyTheme(string themeMode, string accentColor);
}
