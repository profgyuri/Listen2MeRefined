using System.Diagnostics;
using System.IO;

namespace Listen2MeRefined.WPF.ErrorHandling;

public sealed class LocalAppDataLogLocationService : ILogLocationService
{
    private static readonly string BaseDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Listen2MeRefined",
        "Logs");

    public string LogDirectoryPath => BaseDirectory;

    public string LogFilePath => Path.Combine(LogDirectoryPath, "log-.txt");

    public void EnsureLogDirectoryExists()
    {
        Directory.CreateDirectory(LogDirectoryPath);
    }

    public void OpenLogDirectory()
    {
        EnsureLogDirectoryExists();

        Process.Start(new ProcessStartInfo
        {
            FileName = LogDirectoryPath,
            UseShellExecute = true
        });
    }
}
