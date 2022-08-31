namespace Listen2MeRefined.Core.Interfaces;

using Listen2MeRefined.Core.Models;

public interface ISettingsManager
{
    SettingsModel Settings { get; }
    void Save(Action<SettingsModel> settings);
    SettingsModel Load();
}