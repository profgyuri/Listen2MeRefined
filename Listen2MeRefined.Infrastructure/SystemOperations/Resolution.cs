namespace Listen2MeRefined.Infrastructure.SystemOperations;
using Microsoft.Win32;

public static class Resolution
{
    public static float GetScaleFactor()
    {
        const string regKeyName = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ThemeManager";
        var dpi = int.Parse((string)Registry.GetValue(regKeyName, "LastLoadedDPI", "96"));
        return dpi / 96f;
    }
}