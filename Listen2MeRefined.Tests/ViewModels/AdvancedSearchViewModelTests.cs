using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Searching;
using Listen2MeRefined.Infrastructure.Settings;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.ViewModels;

public class AdvancedSearchViewModelTests
{
    [Fact]
    public async Task AddCriteria_InvalidNumericInput_ShowsValidationAndDoesNotAdd()
    {
        var (vm, _) = await CreateViewModelAsync();

        vm.SelectedColumnName = nameof(AudioModel.BPM);
        vm.SelectedRelation = "Bigger than";
        vm.InputText = "abc";
        vm.AddCriteriaCommand.Execute(null);

        Assert.Empty(vm.Criterias);
        Assert.Contains("whole number", vm.ValidationMessage);
    }

    [Fact]
    public async Task AddCriteria_LengthInput_NormalizesToTimespanFormat()
    {
        var (vm, _) = await CreateViewModelAsync();

        vm.SelectedColumnName = nameof(AudioModel.Length);
        vm.SelectedRelation = "Is";
        vm.InputText = "03:05";
        vm.AddCriteriaCommand.Execute(null);

        var criterion = Assert.Single(vm.Criterias);
        Assert.Equal("00:03:05", criterion.NormalizedValue);
        Assert.Equal("03:05", criterion.RawValue);
        Assert.Equal(string.Empty, vm.InputText);
    }

    [Fact]
    public async Task SearchCommand_UsesMatchAnyAndDoesNotClearCriteria()
    {
        var (vm, messenger) = await CreateViewModelAsync();

        vm.SelectedColumnName = nameof(AudioModel.Title);
        vm.SelectedRelation = "Contains";
        vm.InputText = "test";
        vm.AddCriteriaCommand.Execute(null);

        Assert.True(vm.SearchCommand.CanExecute(null));
        vm.IsMatchAny = true;

        AdvancedSearchRequestedMessage? captured = null;
        var recipient = new object();
        messenger.Register<object, AdvancedSearchRequestedMessage>(recipient, (_, message) => captured = message);

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.NotNull(captured);
        Assert.Equal(SearchMatchMode.Any, captured!.Value.MatchMode);
        Assert.Single(vm.Criterias);
        Assert.NotEqual("Searching...", vm.SearchStatusMessage);
    }

    [Fact]
    public async Task SearchCompletedMessage_UpdatesResultStatus()
    {
        var (vm, messenger) = await CreateViewModelAsync();

        messenger.Send(new AdvancedSearchCompletedMessage(0));

        Assert.False(vm.HasSearchResults);
        Assert.Contains("No matches found", vm.SearchStatusMessage);

        messenger.Send(new AdvancedSearchCompletedMessage(4));

        Assert.True(vm.HasSearchResults);
        Assert.Equal(4, vm.LastSearchResultCount);
        Assert.Contains("Found 4 result(s).", vm.SearchStatusMessage);
    }

    private static async Task<(AdvancedSearchShellDefaultHomeViewModel ViewModel, IMessenger Messenger)> CreateViewModelAsync()
    {
        var logger = new Mock<ILogger>();
        logger
            .Setup(x => x.ForContext(It.IsAny<Type>()))
            .Returns(logger.Object);
        var settings = new Mock<ISettingsManager<AppSettings>>();
        var ui = new Mock<IUiDispatcher>();
        var messenger = new WeakReferenceMessenger();
        settings.SetupGet(s => s.Settings).Returns(new AppSettings { FontFamily = "Segoe UI" });
        ui.Setup(x => x.InvokeAsync(It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns<Action, CancellationToken>((action, _) =>
            {
                action();
                return Task.CompletedTask;
            });
        ui.Setup(x => x.InvokeAsync(It.IsAny<Func<bool>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<bool>, CancellationToken>((func, _) => Task.FromResult(func()));
        var settingsReadService = new AppSettingsReader(settings.Object);
        var criteriaService = new AdvancedSearchCriteriaService();

        var vm = new AdvancedSearchShellDefaultHomeViewModel(
            Mock.Of<IErrorHandler>(),
            logger.Object,
            messenger,
            ui.Object,
            criteriaService,
            settingsReadService);
        await vm.InitializeAsync();
        return (vm, messenger);
    }
}
