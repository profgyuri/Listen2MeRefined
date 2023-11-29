namespace Listen2MeRefined.Infrastructure.Storage;
using Newtonsoft.Json;

/// <summary>
///     Responsible to save the specified settings to a file in .json format.
/// </summary>
/// <typeparam name="T">The type of the settings.</typeparam>
public sealed class FileSettingsManager<T> : ISettingsManager<T>
    where T : Settings, new()
{
    private T? _settings;
    private const string SettingsFileName = "settings.json";

    public FileSettingsManager()
    {
        if (File.Exists(SettingsFileName))
        {
            LoadSettings();
            return;
        }

        // If the settings file does not exist, create a new one.
        SaveSettings(_ => { });
    }

    private static T LoadSettings()
    {
        var text = File.ReadAllText(SettingsFileName);
        return JsonConvert.DeserializeObject<T>(text)!;
    }

    #region Implementation of ISettingsManager<out T>
    /// <inheritdoc />
    public T Settings => _settings ??= LoadSettings();

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="settings" /> is <see langword="null" />.</exception>
    public void SaveSettings(Action<T>? settings = null)
    {
        settings?.Invoke(_settings ??= new T());

        var text = JsonConvert.SerializeObject(_settings);
        File.WriteAllText(SettingsFileName, text);
    }
    #endregion
}