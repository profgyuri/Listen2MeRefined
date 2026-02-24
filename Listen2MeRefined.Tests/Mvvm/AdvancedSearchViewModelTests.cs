using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Models;
using Listen2MeRefined.Infrastructure.Mvvm;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Services;
using Listen2MeRefined.Infrastructure.Settings;
using MediatR;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Mvvm;

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
    }

    [Fact]
    public async Task SearchCommand_UsesMatchAnyAndDoesNotClearCriteria()
    {
        var (vm, mediator) = await CreateViewModelAsync();

        vm.SelectedColumnName = nameof(AudioModel.Title);
        vm.SelectedRelation = "Contains";
        vm.InputText = "test";
        vm.AddCriteriaCommand.Execute(null);

        Assert.True(vm.SearchCommand.CanExecute(null));
        vm.IsMatchAny = true;

        AdvancedSearchNotification? captured = null;
        mediator
            .Setup(m => m.Publish(It.IsAny<AdvancedSearchNotification>(), It.IsAny<CancellationToken>()))
            .Callback<AdvancedSearchNotification, CancellationToken>((notification, _) => captured = notification)
            .Returns(Task.CompletedTask);

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.NotNull(captured);
        Assert.Equal(SearchMatchMode.Any, captured!.MatchMode);
        Assert.Single(vm.Criterias);
        Assert.NotEqual("Searching...", vm.SearchStatusMessage);
    }

    [Fact]
    public async Task Handle_SearchCompletedNotification_UpdatesResultStatus()
    {
        var (vm, _) = await CreateViewModelAsync();

        await vm.Handle(new AdvancedSearchCompletedNotification(0), CancellationToken.None);

        Assert.False(vm.HasSearchResults);
        Assert.Contains("No matches found", vm.SearchStatusMessage);

        await vm.Handle(new AdvancedSearchCompletedNotification(4), CancellationToken.None);

        Assert.True(vm.HasSearchResults);
        Assert.Equal(4, vm.LastSearchResultCount);
        Assert.Contains("Found 4 result(s).", vm.SearchStatusMessage);
    }

    private static async Task<(AdvancedSearchViewModel ViewModel, Mock<IMediator> Mediator)> CreateViewModelAsync()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger>();
        var settings = new Mock<ISettingsManager<AppSettings>>();
        var ui = new Mock<IUiDispatcher>();
        settings.SetupGet(s => s.Settings).Returns(new AppSettings { FontFamily = "Segoe UI" });
        var settingsReadService = new AppSettingsReadService(settings.Object);
        var criteriaService = new AdvancedSearchCriteriaService();

        var vm = new AdvancedSearchViewModel(
            mediator.Object,
            logger.Object,
            ui.Object,
            settingsReadService,
            criteriaService);
        await vm.InitializeAsync();
        return (vm, mediator);
    }
}
