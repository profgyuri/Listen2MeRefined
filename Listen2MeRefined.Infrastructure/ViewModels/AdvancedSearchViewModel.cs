using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Searching;

namespace Listen2MeRefined.Infrastructure.ViewModels;

public partial class AdvancedSearchViewModel :
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchCompletedNotification>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly IUiDispatcher _ui;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAdvancedSearchCriteriaService _criteriaService;

    [ObservableProperty] private List<string> _columnName = [];
    [ObservableProperty] private List<string> _relation = [];
    [ObservableProperty] private string _selectedRelation = string.Empty;
    [ObservableProperty] private ObservableCollection<AdvancedSearchCriterion> _criterias = [];
    [ObservableProperty] private AdvancedSearchCriterion? _selectedCriteria;
    [ObservableProperty] private string _rangeSuffixText = string.Empty;
    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private SearchMatchMode _matchMode = SearchMatchMode.All;
    [ObservableProperty] private string _validationMessage = string.Empty;
    [ObservableProperty] private string _searchStatusMessage = "Add at least one filter to search.";
    [ObservableProperty] private int _lastSearchResultCount;
    [ObservableProperty] private bool _hasSearchResults;

    public string SelectedColumnName
    {
        get;
        set
        {
            field = value;
            var relationDefinition = _criteriaService.GetRelationDefinition(value);
            Relation = relationDefinition.Relations.ToList();
            RangeSuffixText = relationDefinition.RangeSuffixText;

            if (Relation.Count > 0)
            {
                SelectedRelation = Enumerable.First<string>(Relation);
            }

            OnPropertyChanged();
            _ui.InvokeAsync(() => AddCriteriaCommand.NotifyCanExecuteChanged());
        }
    }

    public bool IsMatchAll
    {
        get => MatchMode == SearchMatchMode.All;
        set
        {
            if (value)
            {
                MatchMode = SearchMatchMode.All;
            }
        }
    }

    public bool IsMatchAny
    {
        get => MatchMode == SearchMatchMode.Any;
        set
        {
            if (value)
            {
                MatchMode = SearchMatchMode.Any;
            }
        }
    }
    
    public FontFamily FontFamily
    {
        set => SetProperty(ref field, value);
        get => field ??= new FontFamily("Segoe UI");
    }

    public AdvancedSearchViewModel(
        IMediator mediator,
        ILogger logger,
        IUiDispatcher ui,
        IAppSettingsReader settingsReader,
        IAdvancedSearchCriteriaService criteriaService)
    {
        _mediator = mediator;
        _logger = logger;
        _ui = ui;
        _settingsReader = settingsReader;
        _criteriaService = criteriaService;
        SelectedColumnName = string.Empty;
        Criterias.CollectionChanged += OnCriteriasChanged;

        _logger.Debug("[AdvancedSearchViewModel] initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        await _ui.InvokeAsync(() => Criterias.Clear(), ct);

        await Task.Run(() =>
        {
            FontFamily = new FontFamily(_settingsReader.GetFontFamily());
            ColumnName = _criteriaService.GetColumnNames().ToList();
            SelectedColumnName = Enumerable.FirstOrDefault<string>(ColumnName) ?? string.Empty;
            MatchMode = SearchMatchMode.All;
            ValidationMessage = string.Empty;
            SearchStatusMessage = "Add at least one filter to search.";
            LastSearchResultCount = 0;
            HasSearchResults = false;
        }, ct);

        _logger.Debug("[AdvancedSearchViewModel] Finished InitializeCoreAsync");
    }

    [RelayCommand(CanExecute = nameof(CanAddCriteria))]
    private void AddCriteria()
    {
        var result = _criteriaService.BuildCriterion(SelectedColumnName, SelectedRelation, InputText);
        if (!result.Success || result.Criterion is null)
        {
            ValidationMessage = result.ErrorMessage;
            _logger.Warning("[AdvancedSearchViewModel] Cannot add criteria. {Error}", result.ErrorMessage);
            return;
        }

        Criterias.Add(result.Criterion);
        SelectedCriteria = result.Criterion;
        ValidationMessage = string.Empty;
        SearchStatusMessage = $"{Criterias.Count} filter(s) ready.";

        _logger.Debug("[AdvancedSearchViewModel] Added criteria: {@Filter}", result.Criterion);
        InputText = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteItem))]
    private void DeleteItem(AdvancedSearchCriterion? criterion = null)
    {
        var target = criterion ?? SelectedCriteria;
        if (target is null)
        {
            return;
        }

        Criterias.Remove(target);
        if (ReferenceEquals(SelectedCriteria, target))
        {
            SelectedCriteria = null;
        }

        ValidationMessage = string.Empty;
        SearchStatusMessage = Criterias.Count == 0
            ? "Add at least one filter to search."
            : $"{Criterias.Count} filter(s) ready.";
        _logger.Debug("[AdvancedSearchViewModel] Deleted criteria: {Criteria}", target.Display);
    }

    [RelayCommand(CanExecute = nameof(CanEditCriteria))]
    private void EditCriteria(AdvancedSearchCriterion? criterion = null)
    {
        var target = criterion ?? SelectedCriteria;
        if (target is null)
        {
            return;
        }

        SelectedColumnName = target.Field;
        SelectedRelation = target.Relation;
        InputText = target.RawValue;
        Criterias.Remove(target);
        SelectedCriteria = null;
        ValidationMessage = "Editing filter. Press + or Enter to apply.";
        SearchStatusMessage = Criterias.Count == 0
            ? "Add at least one filter to search."
            : $"{Criterias.Count} filter(s) ready.";
    }

    [RelayCommand(CanExecute = nameof(CanDuplicateCriteria))]
    private void DuplicateCriteria(AdvancedSearchCriterion? criterion = null)
    {
        var target = criterion ?? SelectedCriteria;
        if (target is null)
        {
            return;
        }

        var copy = target with { };
        Criterias.Add(copy);
        SelectedCriteria = copy;
        ValidationMessage = string.Empty;
        SearchStatusMessage = $"{Criterias.Count} filter(s) ready.";
    }

    [RelayCommand(CanExecute = nameof(CanClearAll))]
    private void ClearAll()
    {
        Criterias.Clear();
        SelectedCriteria = null;
        ValidationMessage = string.Empty;
        SearchStatusMessage = "Filters cleared.";
        LastSearchResultCount = 0;
        HasSearchResults = false;
    }

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task Search()
    {
        if (Criterias.Count == 0)
        {
            ValidationMessage = "Add at least one filter before searching.";
            return;
        }

        ValidationMessage = string.Empty;
        SearchStatusMessage = "Searching...";
        var filters = _criteriaService.BuildFilters(Criterias).ToList();

        _logger.Information("Starting advanced search with these filters: {@Filters}", filters);
        await _mediator.Publish(new AdvancedSearchNotification(filters, MatchMode));

        if (SearchStatusMessage == "Searching...")
        {
            SearchStatusMessage = "Search completed. Check Search Results.";
        }
    }

    public Task SearchAsync()
    {
        return SearchCommand.ExecuteAsync(null);
    }

    private bool CanAddCriteria()
    {
        return _criteriaService.CanBuildCriterion(SelectedColumnName, SelectedRelation, InputText);
    }

    private bool CanDeleteItem(AdvancedSearchCriterion? criterion)
    {
        return criterion is not null || SelectedCriteria is not null;
    }

    private bool CanEditCriteria(AdvancedSearchCriterion? criterion)
    {
        return criterion is not null || SelectedCriteria is not null;
    }

    private bool CanDuplicateCriteria(AdvancedSearchCriterion? criterion)
    {
        return criterion is not null || SelectedCriteria is not null;
    }

    private bool CanClearAll()
    {
        return Criterias.Count > 0;
    }

    private bool CanSearch()
    {
        return Criterias.Count > 0;
    }

    partial void OnSelectedRelationChanged(string value)
    {
        AddCriteriaCommand.NotifyCanExecuteChanged();
    }

    partial void OnInputTextChanged(string value)
    {
        AddCriteriaCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedCriteriaChanged(AdvancedSearchCriterion? value)
    {
        DeleteItemCommand.NotifyCanExecuteChanged();
        EditCriteriaCommand.NotifyCanExecuteChanged();
        DuplicateCriteriaCommand.NotifyCanExecuteChanged();
    }

    partial void OnMatchModeChanged(SearchMatchMode value)
    {
        OnPropertyChanged(nameof(IsMatchAll));
        OnPropertyChanged(nameof(IsMatchAny));
    }

    private void OnCriteriasChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _ui.InvokeAsync(() =>
        {
            SearchCommand.NotifyCanExecuteChanged();
            ClearAllCommand.NotifyCanExecuteChanged();
            DeleteItemCommand.NotifyCanExecuteChanged();
            EditCriteriaCommand.NotifyCanExecuteChanged();
            DuplicateCriteriaCommand.NotifyCanExecuteChanged();
        });
    }

    public Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[AdvancedSearchViewModel] Received FontFamilyChangedNotification: {FontFamily}", notification.FontFamily);
        FontFamily = new FontFamily(notification.FontFamily);
        return Task.CompletedTask;
    }

    public Task Handle(AdvancedSearchCompletedNotification notification, CancellationToken cancellationToken)
    {
        LastSearchResultCount = notification.ResultCount;
        HasSearchResults = notification.ResultCount > 0;
        SearchStatusMessage = notification.ResultCount > 0
            ? $"Found {notification.ResultCount} result(s)."
            : "No matches found. Adjust filters and try again.";
        return Task.CompletedTask;
    }
}
