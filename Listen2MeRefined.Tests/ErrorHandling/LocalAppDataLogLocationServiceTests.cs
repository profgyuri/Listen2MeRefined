using System.IO;
using Listen2MeRefined.WPF.ErrorHandling;

namespace Listen2MeRefined.Tests.ErrorHandling;

public sealed class LocalAppDataLogLocationServiceTests
{
    [Fact]
    public void LogDirectoryPath_State_ResolvesUnderLocalAppData()
    {
        var sut = new LocalAppDataLogLocationService();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(localAppData, sut.LogDirectoryPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            Path.Combine("Listen2MeRefined", "Logs"),
            sut.LogDirectoryPath,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogFilePath_State_UsesRollingLogPattern()
    {
        var sut = new LocalAppDataLogLocationService();

        Assert.Equal(Path.Combine(sut.LogDirectoryPath, "log-.txt"), sut.LogFilePath);
    }
}
