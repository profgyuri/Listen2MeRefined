namespace Listen2MeRefined.Infrastructure.Data;

using Listen2MeRefined.Core.Models;
using Newtonsoft.Json;

/// <summary>
///     Responsible to save app settings to a file.
/// </summary>
public class FileSettingsManager : ISettingsManager
{
    private const string SettingsFileName = "settings.json";

    private readonly ILogger _logger;

    public FileSettingsManager(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///    Loads the settings from the file.
    /// </summary>
    /// <returns></returns>
    public SettingsModel Load()
    {
        _logger.Debug("Loading settings...");

        var settings = new SettingsModel();

        try
        {
            var text = File.ReadAllText(SettingsFileName);
            settings = JsonConvert.DeserializeObject<SettingsModel>(text)!;
        }
        catch (FileNotFoundException ex)
        {
            _logger.Error(ex, "Settings file not found.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Couldn't load settings!");
        }

        return settings;
    }

    public void Save(SettingsModel settings)
    {
        _logger.Debug("Saving settings...");

        try
        {
            var text = JsonConvert.SerializeObject(settings);
            File.WriteAllText(SettingsFileName, text);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Couldn't save settings!");
        }
    }
}