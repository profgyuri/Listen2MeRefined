using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Mvvm;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;
using Moq;
using Serilog;

namespace Listen2MeRefined.Tests.Mvvm;

public class AdvancedSearchViewModelTests : IClassFixture<AdvancedSearchTestFixture>
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ISettingsManager<AppSettings>> _settingsManagerMock;
    private AdvancedSearchViewModel _viewModel;

    public AdvancedSearchViewModelTests(AdvancedSearchTestFixture fixture)
    {
        _mediatorMock = fixture.MediatorMock;
        _loggerMock = fixture.LoggerMock;
        _settingsManagerMock = fixture.SettingsManagerMock;
        InitializeViewModel();
        
        // Wait for the ViewModel's initialization to complete
        // This is important because AdvancedSearchViewModel.Initialize() is async
        //Task.Run(_viewModel.Initialize).Wait();
    }

    private void InitializeViewModel()
    {
        _viewModel = new AdvancedSearchViewModel(
            _mediatorMock.Object,
            _loggerMock.Object,
            _settingsManagerMock.Object);

        // Wait for the ViewModel to be ready
        WaitForViewModelReady();
    }

    private void WaitForViewModelReady()
    {
        // Wait for ColumnName to be populated
        var timeout = DateTime.Now.AddSeconds(5);
        while (_viewModel.ColumnName == null || !_viewModel.ColumnName.Any())
        {
            if (DateTime.Now > timeout)
            {
                throw new TimeoutException("ViewModel initialization timed out");
            }
            Thread.Sleep(100);
        }

        // Ensure first values are selected
        if (string.IsNullOrEmpty(_viewModel.SelectedColumnName))
        {
            _viewModel.SelectedColumnName = _viewModel.ColumnName.First();
        }
        if (string.IsNullOrEmpty(_viewModel.SelectedRelation))
        {
            _viewModel.SelectedRelation = _viewModel.Relation.First();
        }
    }

    private void ResetViewModelState()
    {
        _viewModel.InputText = string.Empty;
        _viewModel.Criterias.Clear();
        _viewModel.SelectedCriteria = null;
        WaitForViewModelReady();
    }

    [Fact]
    public void AddCriteria_WithNumericComparison_GeneratesCorrectSqlFragment()
    {
        // Arrange
        ResetViewModelState();
        _viewModel.SelectedColumnName = "BPM";
        _viewModel.SelectedRelation = "Bigger than";
        _viewModel.InputText = "120";

        // Act
        _viewModel.AddCriteriaCommand.Execute(null);

        // Assert
        Assert.Single(_viewModel.Criterias);
        Assert.Equal("BPM Bigger than 120", _viewModel.Criterias[0]);
    }

    [Fact]
    public void AddCriteria_WithContains_GeneratesCorrectSqlFragment()
    {
        // Arrange
        ResetViewModelState();
        _viewModel.SelectedColumnName = "Title";
        _viewModel.SelectedRelation = "Contains";
        _viewModel.InputText = "test";

        // Act
        _viewModel.AddCriteriaCommand.Execute(null);

        // Assert
        Assert.Single(_viewModel.Criterias);
        Assert.Equal("Title Contains test", _viewModel.Criterias[0]);
    }

    [Fact]
    public void AddCriteria_WithDoesNotContain_GeneratesCorrectSqlFragment()
    {
        // Arrange
        ResetViewModelState();
        _viewModel.SelectedColumnName = "Artist";
        _viewModel.SelectedRelation = "Does not contain";
        _viewModel.InputText = "test";

        // Act
        _viewModel.AddCriteriaCommand.Execute(null);

        // Assert
        Assert.Single(_viewModel.Criterias);
        Assert.Equal("Artist Does not contain test", _viewModel.Criterias[0]);
    }

    [Fact]
    public void DeleteCriteria_RemovesCorrectItem()
    {
        // Arrange
        ResetViewModelState();
        _viewModel.SelectedColumnName = "Title";
        _viewModel.SelectedRelation = "Contains";
        _viewModel.InputText = "test";
        _viewModel.AddCriteriaCommand.Execute(null);
        _viewModel.SelectedCriteria = _viewModel.Criterias[0];

        // Act
        _viewModel.DeleteItemCommand.Execute(null);

        // Assert
        Assert.Empty(_viewModel.Criterias);
    }
}