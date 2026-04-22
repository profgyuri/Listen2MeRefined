using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Playlist.Formats;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Playlist;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Playlist;

public class ExternalDropImportServiceRoutingTests : IDisposable
{
    private readonly string _tempDir;

    public ExternalDropImportServiceRoutingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "l2m-drop-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures in tests.
        }
    }

    [Fact]
    public async Task HandleExternalFileDropAsync_OnlyPlaylistFile_RoutesToImportService()
    {
        var playlistPath = CreateFile("playlist.m3u");
        var (sut, harness) = CreateSut(playlistExtensions: [".m3u"]);

        await sut.HandleExternalFileDropAsync([playlistPath], insertIndex: 0);

        harness.ImportServiceMock.Verify(
            x => x.ImportAsync(playlistPath, It.IsAny<CancellationToken>()),
            Times.Once);
        harness.FileScannerMock.Verify(
            x => x.ScanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleExternalFileDropAsync_MultiplePlaylistFiles_ImportsFirstOnly()
    {
        var firstPath = CreateFile("first.m3u");
        var secondPath = CreateFile("second.pls");
        var (sut, harness) = CreateSut(playlistExtensions: [".m3u", ".pls"]);

        await sut.HandleExternalFileDropAsync([firstPath, secondPath], insertIndex: 0);

        harness.ImportServiceMock.Verify(
            x => x.ImportAsync(firstPath, It.IsAny<CancellationToken>()),
            Times.Once);
        harness.ImportServiceMock.Verify(
            x => x.ImportAsync(secondPath, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleExternalFileDropAsync_MixedAudioAndPlaylist_IgnoresPlaylistImportsAudio()
    {
        var audioPath = CreateFile("track.mp3");
        var playlistPath = CreateFile("bundle.m3u");
        var (sut, harness) = CreateSut(playlistExtensions: [".m3u"]);

        harness.FileScannerMock
            .Setup(x => x.ScanAsync(audioPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioModel { Path = audioPath, Title = "Track" });

        await sut.HandleExternalFileDropAsync([audioPath, playlistPath], insertIndex: 0);

        harness.ImportServiceMock.Verify(
            x => x.ImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        harness.FileScannerMock.Verify(
            x => x.ScanAsync(audioPath, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Single(harness.QueueState.DefaultPlaylist);
        Assert.Equal(audioPath, harness.QueueState.DefaultPlaylist[0].Path);
    }

    private string CreateFile(string name)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, "dummy");
        return path;
    }

    private (ExternalDropImportService Service, Harness Harness) CreateSut(IReadOnlyList<string> playlistExtensions)
    {
        var extSet = new HashSet<string>(playlistExtensions, StringComparer.OrdinalIgnoreCase);

        var formatMock = new Mock<IPlaylistFileFormat>();
        var registryMock = new Mock<IPlaylistFormatRegistry>();
        registryMock
            .Setup(x => x.ResolveForPath(It.IsAny<string>()))
            .Returns((string p) => extSet.Contains(Path.GetExtension(p)) ? formatMock.Object : null);

        var queueState = new PlaylistQueueState(new PlaylistQueue());
        var fileScanner = new Mock<IFileScanner>();
        var settingsReader = new Mock<IAppSettingsReader>();
        settingsReader.Setup(x => x.GetMusicFolders()).Returns(Array.Empty<string>());
        settingsReader.Setup(x => x.GetMutedDroppedSongFolders()).Returns(Array.Empty<string>());
        var settingsWriter = new Mock<IAppSettingsWriter>();
        var folderPrompt = new Mock<IDroppedSongFolderPromptService>();
        folderPrompt
            .Setup(x => x.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddDroppedSongFolderDecision.SkipAndDontAskAgain);
        var importService = new Mock<IPlaylistImportService>();
        importService
            .Setup(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger>();
        logger.Setup(x => x.ForContext(It.IsAny<Type>())).Returns(logger.Object);

        var service = new ExternalDropImportService(
            queueState,
            fileScanner.Object,
            settingsReader.Object,
            settingsWriter.Object,
            folderPrompt.Object,
            registryMock.Object,
            importService.Object,
            logger.Object);

        return (service, new Harness(
            registryMock,
            fileScanner,
            importService,
            queueState));
    }

    private sealed record Harness(
        Mock<IPlaylistFormatRegistry> RegistryMock,
        Mock<IFileScanner> FileScannerMock,
        Mock<IPlaylistImportService> ImportServiceMock,
        PlaylistQueueState QueueState);
}
