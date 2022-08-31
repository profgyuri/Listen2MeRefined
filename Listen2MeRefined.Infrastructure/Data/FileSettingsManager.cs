namespace Listen2MeRefined.Infrastructure.Data;

using Listen2MeRefined.Core.Models;
using Newtonsoft.Json;

/// <summary>
///     Responsible to save app settings to a file.
/// </summary>
public class FileSettingsManager : ISettingsManager
{
    private const string SettingsFileName = "settings.json";

    private SettingsModel? _settings;

    private readonly ILogger _logger;

    public SettingsModel Settings => _settings ??= Load();

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

        try
        {
            var text = File.ReadAllText(SettingsFileName);
            _settings = JsonConvert.DeserializeObject<SettingsModel>(text)!;
        }
        catch (FileNotFoundException ex)
        {
            _logger.Error(ex, "Settings file not found.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Couldn't load settings!");
        }

        return _settings;
    }

    /// <summary>
    ///     Saves the settings to a file.
    /// </summary>
    /// <param name="settings">Delegete, where you can specify only the settings you want to save.</param>
    /// <example>
    ///     _iSettingsManager.Save(settings => settings.FontFamily = selectedFont);
    /// </example>
    public void Save(Action<SettingsModel> settings)
    {
        _logger.Debug("Saving settings...");

        settings.Invoke(_settings);

        try
        {
            var text = JsonConvert.SerializeObject(_settings);
            File.WriteAllText(SettingsFileName, text);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Couldn't save settings!");
        }
    }
}