namespace Listen2MeRefined.Infrastructure.Data;

using Listen2MeRefined.Core.Models;

public class SettingsManager : ISettingsManager
{
    private readonly ILogger _logger;

    public SettingsManager(ILogger logger)
    {
        _logger = logger;
    }

    public SettingsModel Load()
    {
        throw new NotImplementedException();
    }

    public void Save(SettingsModel settings)
    {
        throw new NotImplementedException();
    }
}