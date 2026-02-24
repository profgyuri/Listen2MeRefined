using Listen2MeRefined.Infrastructure.BackgroundTaskStatusReport;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Data.Repositories;
using Listen2MeRefined.Infrastructure.Scanning;
using Listen2MeRefined.Infrastructure.Scanning.Files;
using Listen2MeRefined.Infrastructure.Scanning.Folders;
using Listen2MeRefined.Infrastructure.Services.Models;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Services;

public sealed class FolderScannerTests
{
    [Fact]
    public async Task ScanAsync_Incremental_AnalyzesOnlyChangedOrNewFiles()
    {
        var folderPath = CreateTempFolder();
        var fileA = Path.Combine(folderPath, "a.mp3");
        var fileB = Path.Combine(folderPath, "b.mp3");
        File.WriteAllText(fileA, "a");
        File.WriteAllText(fileB, "bb");

        try
        {
            var existingA = new AudioModel
            {
                Path = fileA,
                LastWriteUtc = File.GetLastWriteTimeUtc(fileA),
                LengthBytes = new FileInfo(fileA).Length
            };
            var removedC = new AudioModel { Path = Path.Combine(folderPath, "c.mp3") };

            var sut = CreateSut(
                [fileA, fileB],
                [existingA, removedC],
                out var analyzer,
                out var repository,
                out _,
                out _);

            await sut.ScanAsync(
                [new FolderScanRequest(folderPath, false)],
                ScanMode.Incremental,
                CancellationToken.None);

            analyzer.Verify(x => x.AnalyzeAsync(fileA, It.IsAny<CancellationToken>()), Times.Never);
            analyzer.Verify(x => x.AnalyzeAsync(fileB, It.IsAny<CancellationToken>()), Times.Once);
            repository.Verify(
                x => x.PersistScanChangesAsync(
                    It.Is<IReadOnlyCollection<AudioModel>>(toInsert => toInsert.Count == 1 && toInsert.Single().Path == fileB),
                    It.Is<IReadOnlyCollection<AudioModel>>(toUpdate => toUpdate.Count == 0),
                    It.Is<IReadOnlyCollection<AudioModel>>(toRemove => toRemove.Count == 1 && toRemove.Single().Path == removedC.Path)),
                Times.Once);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }

    [Fact]
    public async Task ScanAsync_FullRefresh_AnalyzesAllFiles()
    {
        var folderPath = CreateTempFolder();
        var fileA = Path.Combine(folderPath, "a.mp3");
        var fileB = Path.Combine(folderPath, "b.mp3");
        File.WriteAllText(fileA, "a");
        File.WriteAllText(fileB, "bb");

        try
        {
            var existingA = new AudioModel
            {
                Path = fileA,
                LastWriteUtc = File.GetLastWriteTimeUtc(fileA),
                LengthBytes = new FileInfo(fileA).Length
            };

            var sut = CreateSut(
                [fileA, fileB],
                [existingA],
                out var analyzer,
                out var repository,
                out _,
                out _);

            await sut.ScanAsync(
                [new FolderScanRequest(folderPath, false)],
                ScanMode.FullRefresh,
                CancellationToken.None);

            analyzer.Verify(x => x.AnalyzeAsync(fileA, It.IsAny<CancellationToken>()), Times.Once);
            analyzer.Verify(x => x.AnalyzeAsync(fileB, It.IsAny<CancellationToken>()), Times.Once);
            repository.Verify(
                x => x.PersistScanChangesAsync(
                    It.Is<IReadOnlyCollection<AudioModel>>(toInsert => toInsert.Count == 1 && toInsert.Single().Path == fileB),
                    It.Is<IReadOnlyCollection<AudioModel>>(toUpdate => toUpdate.Any(song => song.Path == fileA)),
                    It.Is<IReadOnlyCollection<AudioModel>>(toRemove => toRemove.Count == 0)),
                Times.Once);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }

    [Fact]
    public async Task ScanAllAsync_UsesConfiguredRecursionFlags()
    {
        var folderPath = CreateTempFolder();
        var fileA = Path.Combine(folderPath, "a.mp3");
        File.WriteAllText(fileA, "a");

        try
        {
            var settings = new AppSettings
            {
                MusicFolders = [new MusicFolderModel(folderPath, true)]
            };
            var settingsManager = new Mock<ISettingsManager<AppSettings>>();
            settingsManager.SetupGet(x => x.Settings).Returns(settings);

            var fileEnumerator = new Mock<IFileEnumerator>();
            fileEnumerator
                .Setup(x => x.EnumerateFilesAsync(folderPath, true, It.IsAny<CancellationToken>()))
                .Returns(GetFilesAsync([fileA]));

            var analyzer = CreateAnalyzerMock();
            var repository = CreateRepositoryMock();
            repository.Setup(x => x.ReadByFolderScopeAsync(folderPath, true)).ReturnsAsync([]);
            var taskStatus = CreateBackgroundTaskStatusMock();

            var sut = new FolderScanner(
                fileEnumerator.Object,
                analyzer.Object,
                repository.Object,
                settingsManager.Object,
                taskStatus.Object,
                Mock.Of<ILogger>());

            await sut.ScanAllAsync(ScanMode.Incremental, CancellationToken.None);

            fileEnumerator.Verify(x => x.EnumerateFilesAsync(folderPath, true, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }

    [Fact]
    public async Task ScanAsync_CompletesTask_WithMultilineSummaryMessage()
    {
        var folderPath = CreateTempFolder();
        var fileA = Path.Combine(folderPath, "a.mp3");
        File.WriteAllText(fileA, "a");

        try
        {
            var sut = CreateSut(
                [fileA],
                [],
                out _,
                out _,
                out _,
                out var taskStatus);

            await sut.ScanAsync(
                [new FolderScanRequest(folderPath, false)],
                ScanMode.Incremental,
                CancellationToken.None);

            taskStatus.Verify(
                x => x.CompleteTask(
                    It.IsAny<TaskHandle>(),
                    It.Is<string>(message =>
                        message.Contains(Environment.NewLine)
                        && message.Contains("Added:")
                        && message.Contains("Updated:")
                        && message.Contains("Removed:")
                        && message.Contains("Skipped:")
                        && message.Contains("Failed:"))),
                Times.Once);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }

    private static FolderScanner CreateSut(
        IEnumerable<string> files,
        IEnumerable<AudioModel> fromDb,
        out Mock<IFileAnalyzer<AudioModel>> analyzer,
        out Mock<IAudioRepository> repository,
        out Mock<IFileEnumerator> fileEnumerator,
        out Mock<IBackgroundTaskStatusService> backgroundTaskStatus)
    {
        var folderPath = Path.GetDirectoryName(files.First())!;
        analyzer = CreateAnalyzerMock();
        repository = CreateRepositoryMock();
        repository
            .Setup(x => x.ReadByFolderScopeAsync(folderPath, false))
            .ReturnsAsync(fromDb.ToArray());

        fileEnumerator = new Mock<IFileEnumerator>();
        fileEnumerator
            .Setup(x => x.EnumerateFilesAsync(folderPath, false, It.IsAny<CancellationToken>()))
            .Returns(GetFilesAsync(files));

        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(new AppSettings());
        backgroundTaskStatus = CreateBackgroundTaskStatusMock();

        return new FolderScanner(
            fileEnumerator.Object,
            analyzer.Object,
            repository.Object,
            settingsManager.Object,
            backgroundTaskStatus.Object,
            Mock.Of<ILogger>());
    }

    private static Mock<IFileAnalyzer<AudioModel>> CreateAnalyzerMock()
    {
        var analyzer = new Mock<IFileAnalyzer<AudioModel>>();
        analyzer
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, CancellationToken _) => new AudioModel
            {
                Path = path,
                LastWriteUtc = File.GetLastWriteTimeUtc(path),
                LengthBytes = new FileInfo(path).Length
            });
        return analyzer;
    }

    private static Mock<IAudioRepository> CreateRepositoryMock()
    {
        var repository = new Mock<IAudioRepository>();
        repository.Setup(x => x.UpdateAsync(It.IsAny<IEnumerable<AudioModel>>())).Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveAsync(It.IsAny<IEnumerable<AudioModel>>())).Returns(Task.CompletedTask);
        repository.Setup(x => x.RemoveAsync(It.IsAny<IEnumerable<AudioModel>>())).Returns(Task.CompletedTask);
        repository
            .Setup(x => x.PersistScanChangesAsync(
                It.IsAny<IReadOnlyCollection<AudioModel>>(),
                It.IsAny<IReadOnlyCollection<AudioModel>>(),
                It.IsAny<IReadOnlyCollection<AudioModel>>()))
            .Returns(Task.CompletedTask);
        return repository;
    }

    private static Mock<IBackgroundTaskStatusService> CreateBackgroundTaskStatusMock()
    {
        var status = new Mock<IBackgroundTaskStatusService>();
        status
            .Setup(x => x.StartTask(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TaskProgressKind>(),
                It.IsAny<int>()))
            .Returns(new TaskHandle(Guid.NewGuid()));
        status
            .Setup(x => x.RegisterWorker(It.IsAny<TaskHandle>(), It.IsAny<string>(), It.IsAny<int?>()))
            .Returns((TaskHandle taskHandle, string _, int? _) => new WorkerHandle(taskHandle.TaskId, Guid.NewGuid()));
        return status;
    }

    private static string CreateTempFolder()
    {
        var path = Path.Combine(Path.GetTempPath(), $"listen2me-scan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static async IAsyncEnumerable<string> GetFilesAsync(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            yield return file;
            await Task.Yield();
        }
    }
}
