using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using Listen2MeRefined.Infrastructure.Scanning.Files;
using Listen2MeRefined.Infrastructure.Startup;
using Listen2MeRefined.Infrastructure.ViewModels;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Startup.ShellOpen;

public sealed class ExternalAudioOpenServiceTests
{
    [Fact]
    public async Task OpenAsync_UnsupportedExtension_ShowsQuickStatusAndSkips()
    {
        var sut = CreateSut(out var analyzer, out _, out var status, out _, out _);
        var path = Path.Combine(Path.GetTempPath(), $"unsupported-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(path, "x");

        try
        {
            await sut.OpenAsync([path]);

            analyzer.Verify(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            status.Verify(x => x.StartTask("shell-open", It.Is<string>(s => s.Contains("Unsupported file")), TaskProgressKind.Indeterminate, 100), Times.Once);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OpenAsync_ExistingPathInPlaylist_JumpsToExistingIndex()
    {
        var sut = CreateSut(out var analyzer, out var player, out _, out var playlist, out _);
        var path = Path.Combine(Path.GetTempPath(), $"existing-{Guid.NewGuid()}.mp3");
        await File.WriteAllTextAsync(path, "x");

        var existing = new AudioModel { Path = path, Title = "Existing" };
        playlist.Items.Add(existing);

        try
        {
            await sut.OpenAsync([path]);

            analyzer.Verify(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            player.Verify(x => x.JumpToIndexAsync(0), Times.Once);
            Assert.Single(playlist.Items);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OpenAsync_CurrentSongSet_InsertsAfterCurrentAndJumpsToFirstInserted()
    {
        var sut = CreateSut(out var analyzer, out var player, out _, out var playlist, out _);
        var first = new AudioModel { Path = "C:/music/first.mp3", Title = "First" };
        var second = new AudioModel { Path = "C:/music/second.mp3", Title = "Second" };
        playlist.Items.Add(first);
        playlist.Items.Add(second);
        sut.SetCurrentSong(first);

        var newFileA = Path.Combine(Path.GetTempPath(), $"open-a-{Guid.NewGuid()}.mp3");
        var newFileB = Path.Combine(Path.GetTempPath(), $"open-b-{Guid.NewGuid()}.mp3");
        await File.WriteAllTextAsync(newFileA, "x");
        await File.WriteAllTextAsync(newFileB, "x");

        analyzer
            .Setup(x => x.AnalyzeAsync(newFileA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioModel { Path = newFileA, Title = "A" });
        analyzer
            .Setup(x => x.AnalyzeAsync(newFileB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioModel { Path = newFileB, Title = "B" });

        try
        {
            await sut.OpenAsync([newFileA, newFileB]);

            Assert.Equal(4, playlist.Count);
            Assert.Equal(newFileA, playlist[1].Path);
            Assert.Equal(newFileB, playlist[2].Path);
            player.Verify(x => x.JumpToIndexAsync(1), Times.Once);
        }
        finally
        {
            File.Delete(newFileA);
            File.Delete(newFileB);
        }
    }

    private static ExternalAudioOpenService CreateSut(
        out Mock<IFileAnalyzer<AudioModel>> analyzer,
        out Mock<IMusicPlayerController> player,
        out Mock<IBackgroundTaskStatusService> status,
        out Playlist playlist,
        out Mock<IUiDispatcher> ui)
    {
        analyzer = new Mock<IFileAnalyzer<AudioModel>>();
        player = new Mock<IMusicPlayerController>();
        status = new Mock<IBackgroundTaskStatusService>();
        playlist = new Playlist();
        ui = new Mock<IUiDispatcher>();

        ui.Setup(x => x.InvokeAsync(It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns<Action, CancellationToken>((action, _) =>
            {
                action();
                return Task.CompletedTask;
            });

        return new ExternalAudioOpenService(
            Mock.Of<ILogger>(),
            analyzer.Object,
            playlist,
            player.Object,
            status.Object,
            ui.Object);
    }
}
