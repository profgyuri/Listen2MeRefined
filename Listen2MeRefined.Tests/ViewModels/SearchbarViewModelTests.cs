using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.ViewModels.Widgets;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public sealed class SearchbarViewModelTests
{
    [Fact]
    public async Task OnSearchTermChanged_EmptyTerm_SearchesAllSongs()
    {
        var allSongs = new AudioModel[] { new() { Title = "Song A" }, new() { Title = "Song B" } };
        var (viewModel, _, probe, searchService) = CreateViewModel(debounceMs: 50);
        searchService
            .Setup(x => x.ExecuteQuickSearchAsync(""))
            .ReturnsAsync(allSongs);
        await viewModel.InitializeAsync();

        // First set a non-empty term, then clear it
        viewModel.SearchTerm = "x";
        viewModel.SearchTerm = "";

        await Task.Delay(150);

        Assert.NotNull(probe.LastSearchResults);
        Assert.Equal(2, probe.LastSearchResults!.Count());
    }

    [Fact]
    public async Task OnSearchTermChanged_WithText_ExecutesSearchAfterDebounce()
    {
        var results = new AudioModel[] { new() { Title = "Match" } };
        var (viewModel, _, probe, searchService) = CreateViewModel(debounceMs: 50);
        searchService
            .Setup(x => x.ExecuteQuickSearchAsync("test"))
            .ReturnsAsync(results);
        await viewModel.InitializeAsync();

        viewModel.SearchTerm = "test";

        // Should not have results immediately
        Assert.Null(probe.LastSearchResults);

        await Task.Delay(150);

        Assert.NotNull(probe.LastSearchResults);
        var resultsList = probe.LastSearchResults!.ToArray();
        Assert.Single(resultsList);
        Assert.Equal("Match", resultsList[0].Title);
    }

    [Fact]
    public async Task OnSearchTermChanged_RapidChanges_OnlyLastSearchExecutes()
    {
        var (viewModel, _, probe, searchService) = CreateViewModel(debounceMs: 50);
        searchService
            .Setup(x => x.ExecuteQuickSearchAsync(It.IsAny<string>()))
            .ReturnsAsync(Array.Empty<AudioModel>());
        searchService
            .Setup(x => x.ExecuteQuickSearchAsync("final"))
            .ReturnsAsync(new AudioModel[] { new() { Title = "Final Result" } });
        await viewModel.InitializeAsync();

        viewModel.SearchTerm = "f";
        viewModel.SearchTerm = "fi";
        viewModel.SearchTerm = "fin";
        viewModel.SearchTerm = "final";

        await Task.Delay(150);

        searchService.Verify(x => x.ExecuteQuickSearchAsync("f"), Times.Never);
        searchService.Verify(x => x.ExecuteQuickSearchAsync("fi"), Times.Never);
        searchService.Verify(x => x.ExecuteQuickSearchAsync("fin"), Times.Never);
        searchService.Verify(x => x.ExecuteQuickSearchAsync("final"), Times.Once);
    }

    [Fact]
    public async Task SearchDebounceChangedMessage_UpdatesDebounceTime()
    {
        var (viewModel, messenger, probe, searchService) = CreateViewModel(debounceMs: 50);
        searchService
            .Setup(x => x.ExecuteQuickSearchAsync("test"))
            .ReturnsAsync(new AudioModel[] { new() { Title = "Result" } });
        await viewModel.InitializeAsync();

        // Increase debounce to 500ms
        messenger.Send(new SearchDebounceChangedMessage(500));

        viewModel.SearchTerm = "test";

        // After 150ms, search should NOT have fired (debounce is now 500ms)
        await Task.Delay(150);
        Assert.Null(probe.LastSearchResults);

        // After total ~650ms, search should have fired
        await Task.Delay(500);
        Assert.NotNull(probe.LastSearchResults);
    }

    private static (
        SearchbarViewModel ViewModel,
        WeakReferenceMessenger Messenger,
        MessageProbe Probe,
        Mock<IAudioSearchExecutionService> SearchService) CreateViewModel(short debounceMs = 300)
    {
        var settings = new AppSettings { SearchDebounceMs = debounceMs };
        var settingsManager = new Mock<ISettingsManager<AppSettings>>();
        settingsManager.SetupGet(x => x.Settings).Returns(settings);

        var settingsReader = new AppSettingsReader(settingsManager.Object);

        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);

        var errorHandler = new Mock<IErrorHandler>();
        errorHandler
            .Setup(x => x.HandleAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var messenger = new WeakReferenceMessenger();
        var probe = new MessageProbe();
        messenger.Register<MessageProbe, QuickSearchExecutedMessage>(
            probe,
            static (recipient, message) => recipient.LastSearchResults = message.Value);

        var searchService = new Mock<IAudioSearchExecutionService>();

        var viewModel = new SearchbarViewModel(
            errorHandler.Object,
            logger.Object,
            messenger,
            searchService.Object,
            settingsReader,
            Mock.Of<IWindowManager>());

        return (viewModel, messenger, probe, searchService);
    }

    private sealed class MessageProbe
    {
        public IEnumerable<AudioModel>? LastSearchResults { get; set; }
    }
}
