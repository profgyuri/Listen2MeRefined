namespace Listen2MeRefined.WPF.ErrorHandling;

public interface ILogLocationService
{
    string LogDirectoryPath { get; }

    string LogFilePath { get; }

    void EnsureLogDirectoryExists();

    void OpenLogDirectory();
}
