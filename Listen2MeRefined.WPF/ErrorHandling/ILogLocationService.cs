namespace Listen2MeRefined.WPF.ErrorHandling;

/// <summary>
/// Resolves and opens the application log storage location.
/// </summary>
public interface ILogLocationService
{
    /// <summary>
    /// Gets the absolute log directory path.
    /// </summary>
    string LogDirectoryPath { get; }

    /// <summary>
    /// Gets the file sink path pattern used by Serilog.
    /// </summary>
    string LogFilePath { get; }

    /// <summary>
    /// Ensures the log directory exists.
    /// </summary>
    void EnsureLogDirectoryExists();

    /// <summary>
    /// Opens the log directory in the operating system file explorer.
    /// </summary>
    void OpenLogDirectory();
}
