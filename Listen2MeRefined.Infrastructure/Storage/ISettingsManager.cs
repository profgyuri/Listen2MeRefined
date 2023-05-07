namespace Listen2MeRefined.Infrastructure.Storage;

/// <summary>
///     Represents a storage provider.
/// </summary>
/// <typeparam name="T">The type of the settings to store.</typeparam>
public interface ISettingsManager<out T>
    where T : Settings
{
    /// <summary>
    ///     Gets the settings as an object.
    /// </summary>
    T Settings { get; }

    /// <summary>
    ///     Saves only the properties that are specified within the action.
    /// </summary>
    /// <param name="settings">The action that specifies the properties to save.</param>
    void SaveSettings(Action<T>? settings = null);
}