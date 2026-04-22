using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Playlist;
using Listen2MeRefined.Application.Playlist.Formats;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Playlist;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Playlist;

public class PlaylistImportServiceTests : IDisposable
{
    private readonly string _tempDir;

    public PlaylistImportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "l2m-import-tests-" + Guid.NewGuid().ToString("N"));
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
    public async Task ImportAsync_EmptyDefaultPlaylist_LoadsWithoutPrompt()
    {
        var playlistPath = CreatePlaylistFile();
        var audioPath = CreateAudioFile("a.mp3");

        var (sut, harness) = CreateSut([new PlaylistFileEntry(audioPath, "Title", "Artist", null)]);

        await sut.ImportAsync(playlistPath);

        harness.PromptMock.Verify(
            x => x.ConfirmReplaceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Single(harness.QueueState.DefaultPlaylist);
        Assert.Equal(audioPath, harness.QueueState.DefaultPlaylist[0].Path);
    }

    [Fact]
    public async Task ImportAsync_NonEmptyDefaultPlaylist_Confirmed_ReplacesContents()
    {
        var playlistPath = CreatePlaylistFile();
        var existingPath = CreateAudioFile("existing.mp3");
        var importedPath = CreateAudioFile("imported.mp3");

        var (sut, harness) = CreateSut([new PlaylistFileEntry(importedPath, null, null, null)]);
        harness.QueueState.DefaultPlaylist.Add(new AudioModel { Path = existingPath, Title = "Old" });
        harness.PromptMock
            .Setup(x => x.ConfirmReplaceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await sut.ImportAsync(playlistPath);

        harness.PromptMock.Verify(
            x => x.ConfirmReplaceAsync(1, 1, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Single(harness.QueueState.DefaultPlaylist);
        Assert.Equal(importedPath, harness.QueueState.DefaultPlaylist[0].Path);
    }

    [Fact]
    public async Task ImportAsync_NonEmptyDefaultPlaylist_Cancelled_DoesNotMutate()
    {
        var playlistPath = CreatePlaylistFile();
        var existingPath = CreateAudioFile("existing.mp3");
        var importedPath = CreateAudioFile("imported.mp3");

        var (sut, harness) = CreateSut([new PlaylistFileEntry(importedPath, null, null, null)]);
        var existingSong = new AudioModel { Path = existingPath, Title = "Old" };
        harness.QueueState.DefaultPlaylist.Add(existingSong);
        harness.PromptMock
            .Setup(x => x.ConfirmReplaceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await sut.ImportAsync(playlistPath);

        Assert.Single(harness.QueueState.DefaultPlaylist);
        Assert.Same(existingSong, harness.QueueState.DefaultPlaylist[0]);
    }

    [Fact]
    public async Task ImportAsync_DatabaseHit_DoesNotInvokeFileScanner()
    {
        var playlistPath = CreatePlaylistFile();
        var audioPath = CreateAudioFile("known.mp3");

        var (sut, harness) = CreateSut([new PlaylistFileEntry(audioPath, null, null, null)]);
        harness.AudioRepositoryMock
            .Setup(x => x.ReadByPathAsync(audioPath))
            .ReturnsAsync(new AudioModel { Path = audioPath, Title = "From DB" });

        await sut.ImportAsync(playlistPath);

        harness.FileScannerMock.Verify(
            x => x.ScanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Single(harness.QueueState.DefaultPlaylist);
        Assert.Equal("From DB", harness.QueueState.DefaultPlaylist[0].Title);
    }

    [Fact]
    public async Task ImportAsync_DatabaseMiss_FallsBackToFileScanner()
    {
        var playlistPath = CreatePlaylistFile();
        var audioPath = CreateAudioFile("unknown.mp3");

        var (sut, harness) = CreateSut([new PlaylistFileEntry(audioPath, null, null, null)]);
        harness.AudioRepositoryMock
            .Setup(x => x.ReadByPathAsync(audioPath))
            .ReturnsAsync((AudioModel?)null);
        harness.FileScannerMock
            .Setup(x => x.ScanAsync(audioPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioModel { Path = audioPath, Title = "Scanned" });

        await sut.ImportAsync(playlistPath);

        harness.FileScannerMock.Verify(
            x => x.ScanAsync(audioPath, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Single(harness.QueueState.DefaultPlaylist);
        Assert.Equal("Scanned", harness.QueueState.DefaultPlaylist[0].Title);
    }

    [Fact]
    public async Task ImportAsync_MissingFile_IsSkippedAndReportedInStatus()
    {
        var playlistPath = CreatePlaylistFile();
        var presentPath = CreateAudioFile("present.mp3");
        var missingPath = Path.Combine(_tempDir, "ghost.mp3");

        var (sut, harness) = CreateSut(
        [
            new PlaylistFileEntry(presentPath, null, null, null),
            new PlaylistFileEntry(missingPath, null, null, null)
        ]);

        await sut.ImportAsync(playlistPath);

        Assert.Single(harness.QueueState.DefaultPlaylist);
        Assert.Equal(presentPath, harness.QueueState.DefaultPlaylist[0].Path);
        harness.StatusMock.Verify(
            x => x.CompleteTask(It.IsAny<TaskHandle>(), It.Is<string>(s => s != null && s.Contains("1 missing"))),
            Times.Once);
    }

    [Fact]
    public async Task ImportAsync_SuccessfulImport_SendsSelectPlaylistRequestedMessageWithNull()
    {
        var playlistPath = CreatePlaylistFile();
        var audioPath = CreateAudioFile("song.mp3");

        var (sut, harness) = CreateSut([new PlaylistFileEntry(audioPath, null, null, null)]);
        int? capturedValue = null;
        var received = false;
        harness.Messenger.Register<SelectPlaylistRequestedMessage>(this, (_, msg) =>
        {
            received = true;
            capturedValue = msg.Value;
        });

        await sut.ImportAsync(playlistPath);

        Assert.True(received);
        Assert.Null(capturedValue);
    }

    [Fact]
    public async Task ImportAsync_UnsupportedFormat_DoesNothing()
    {
        var playlistPath = Path.Combine(_tempDir, "mystery.xyz");
        await File.WriteAllTextAsync(playlistPath, "content");

        var (sut, harness) = CreateSut([], registerFormat: false);

        await sut.ImportAsync(playlistPath);

        Assert.Empty(harness.QueueState.DefaultPlaylist);
        harness.StatusMock.Verify(
            x => x.StartTask(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TaskProgressKind>(), It.IsAny<int>()),
            Times.Never);
        harness.PromptMock.Verify(
            x => x.ConfirmReplaceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportAsync_NoResolvableEntries_FailsTask()
    {
        var playlistPath = CreatePlaylistFile();
        var missingPath = Path.Combine(_tempDir, "nowhere.mp3");

        var (sut, harness) = CreateSut([new PlaylistFileEntry(missingPath, null, null, null)]);

        await sut.ImportAsync(playlistPath);

        Assert.Empty(harness.QueueState.DefaultPlaylist);
        harness.StatusMock.Verify(
            x => x.FailTask(It.IsAny<TaskHandle>(), It.IsAny<string>()),
            Times.Once);
    }

    private string CreatePlaylistFile()
    {
        var path = Path.Combine(_tempDir, "playlist.m3u");
        File.WriteAllText(path, "#EXTM3U\n");
        return path;
    }

    private string CreateAudioFile(string name)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, "dummy");
        return path;
    }

    private (PlaylistImportService Service, Harness Harness) CreateSut(
        IReadOnlyList<PlaylistFileEntry> entries,
        bool registerFormat = true)
    {
        var formatMock = new Mock<IPlaylistFileFormat>();
        formatMock
            .Setup(x => x.ReadAsync(It.IsAny<Stream>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var registryMock = new Mock<IPlaylistFormatRegistry>();
        registryMock
            .Setup(x => x.ResolveForPath(It.IsAny<string>()))
            .Returns(registerFormat ? formatMock.Object : null);

        var audioRepository = new Mock<IAudioRepository>();
        audioRepository
            .Setup(x => x.ReadByPathAsync(It.IsAny<string>()))
            .ReturnsAsync((AudioModel?)null);

        var fileScanner = new Mock<IFileScanner>();
        fileScanner
            .Setup(x => x.ScanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string p, CancellationToken _) => new AudioModel { Path = p });

        var prompt = new Mock<IReplaceDefaultPlaylistPrompt>();
        prompt
            .Setup(x => x.ConfirmReplaceAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var status = new Mock<IBackgroundTaskStatusService>();
        status
            .Setup(x => x.StartTask(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TaskProgressKind>(), It.IsAny<int>()))
            .Returns(new TaskHandle(Guid.NewGuid()));

        var messenger = new WeakReferenceMessenger();
        var queueState = new PlaylistQueueState(new PlaylistQueue());
        var logger = new Mock<ILogger>();
        logger.Setup(x => x.ForContext(It.IsAny<Type>())).Returns(logger.Object);

        var service = new PlaylistImportService(
            registryMock.Object,
            queueState,
            audioRepository.Object,
            fileScanner.Object,
            prompt.Object,
            status.Object,
            messenger,
            logger.Object);

        return (service, new Harness(
            registryMock,
            audioRepository,
            fileScanner,
            prompt,
            status,
            messenger,
            queueState));
    }

    private sealed record Harness(
        Mock<IPlaylistFormatRegistry> RegistryMock,
        Mock<IAudioRepository> AudioRepositoryMock,
        Mock<IFileScanner> FileScannerMock,
        Mock<IReplaceDefaultPlaylistPrompt> PromptMock,
        Mock<IBackgroundTaskStatusService> StatusMock,
        WeakReferenceMessenger Messenger,
        PlaylistQueueState QueueState);
}
