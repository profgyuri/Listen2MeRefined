using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Infrastructure.Settings;

namespace Listen2MeRefined.Infrastructure.Startup.Tasks;

public sealed class ThemeStartupTask : IStartupTask
{
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppThemeService _appThemeService;

    public ThemeStartupTask(IAppSettingsReader settingsReader, IAppThemeService appThemeService)
    {
        _settingsReader = settingsReader;
        _appThemeService = appThemeService;
    }

    public Task RunAsync(CancellationToken ct)
    {
        _appThemeService.ApplyTheme(_settingsReader.GetThemeMode(), _settingsReader.GetAccentColor());
        return Task.CompletedTask;
    }
}
