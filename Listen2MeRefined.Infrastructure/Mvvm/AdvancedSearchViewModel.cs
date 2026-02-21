using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public partial class AdvancedSearchViewModel :
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>,
    INotificationHandler<AdvancedSearchCompletedNotification>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IUiDispatcher _ui;
    private readonly List<string> _numericRelations = ["Is", "Is not", "Bigger than", "Less than"];
    private readonly List<string> _timeRelations = ["Is", "Is not", "More than", "Less than"];
    private readonly List<string> _stringRelations = ["Is", "Is not", "Contains", "Does not contain"];

    [ObservableProperty] private string _fontFamily = string.Empty;
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
            switch (value)
            {
                case nameof(AudioModel.Length):
                    Relation = _timeRelations;
                    RangeSuffixText = "mm:ss or sec";
                    break;
                case nameof(AudioModel.Bitrate):
                    Relation = _numericRelations;
                    RangeSuffixText = "kbps";
                    break;
                case nameof(AudioModel.BPM):
                    Relation = _numericRelations;
                    RangeSuffixText = "bpm";
                    break;
                default:
                    Relation = _stringRelations;
                    RangeSuffixText = string.Empty;
                    break;
            }

            if (Relation.Count > 0)
            {
                SelectedRelation = Relation.First();
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

    public AdvancedSearchViewModel(
        IMediator mediator,
        ILogger logger,
        ISettingsManager<AppSettings> settingsManager, IUiDispatcher ui)
    {
        _mediator = mediator;
        _logger = logger;
        _settingsManager = settingsManager;
        _ui = ui;
        SelectedColumnName = string.Empty;
        Criterias.CollectionChanged += OnCriteriasChanged;

        _logger.Debug("[AdvancedSearchViewModel] initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        await _ui.InvokeAsync(() => Criterias.Clear(), ct);
        
        await Task.Run(() =>
        {
            FontFamily = _settingsManager.Settings.FontFamily;
            ColumnName = GetAudioModelProperties();
            SelectedColumnName = ColumnName.FirstOrDefault() ?? string.Empty;
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
        if (!TryCreateCriterion(out var criterion, out var error))
        {
            ValidationMessage = error;
            _logger.Warning("[AdvancedSearchViewModel] Cannot add criteria. {Error}", error);
            return;
        }

        Criterias.Add(criterion);
        SelectedCriteria = criterion;
        ValidationMessage = string.Empty;
        SearchStatusMessage = $"{Criterias.Count} filter(s) ready.";

        _logger.Debug("[AdvancedSearchViewModel] Added criteria: {@Filter}", criterion);
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
        var filters = Criterias
            .Select(c => new AdvancedFilter(c.Field, c.Operator, c.NormalizedValue))
            .ToList();

        _logger.Information("Starting advanced search with these filters: {@Filters}", filters);
        await _mediator.Publish(new AdvancedSearchNotification(filters, MatchMode));

        // Fallback in case no completion notification is received in this scope.
        if (SearchStatusMessage == "Searching...")
        {
            SearchStatusMessage = "Search completed. Check Search Results.";
        }
    }

    public Task SearchAsync()
    {
        return SearchCommand.ExecuteAsync(null);
    }

    private static AdvancedFilterOperator MapOperator(string relation)
    {
        return relation switch
        {
            "Is" => AdvancedFilterOperator.Equal,
            "Is not" => AdvancedFilterOperator.NotEqual,
            "Contains" => AdvancedFilterOperator.Contains,
            "Does not contain" => AdvancedFilterOperator.NotContains,
            "Bigger than" or "More than" => AdvancedFilterOperator.GreaterThan,
            "Less than" => AdvancedFilterOperator.LessThan,
            _ => throw new IndexOutOfRangeException($"This relation is not handled: {relation}")
        };
    }

    private static List<string> GetAudioModelProperties()
    {
        return typeof(AudioModel)
            .GetProperties()
            .Where(p =>
                p.Name != nameof(AudioModel.Display) &&
                p.Name != nameof(AudioModel.Id))
            .Select(p => p.Name)
            .ToList();
    }

    private bool TryCreateCriterion(out AdvancedSearchCriterion criterion, out string error)
    {
        criterion = default!;
        if (string.IsNullOrWhiteSpace(SelectedColumnName) ||
            string.IsNullOrWhiteSpace(SelectedRelation) ||
            string.IsNullOrWhiteSpace(InputText))
        {
            error = "Please select field, relation, and value.";
            return false;
        }

        if (!TryNormalizeInput(InputText, SelectedColumnName, out var normalizedValue, out error))
        {
            return false;
        }

        AdvancedFilterOperator filterOperator;
        try
        {
            filterOperator = MapOperator(SelectedRelation);
        }
        catch (IndexOutOfRangeException)
        {
            error = "Selected relation is invalid for this field.";
            return false;
        }

        criterion = new AdvancedSearchCriterion(
            SelectedColumnName,
            SelectedRelation,
            InputText.Trim(),
            normalizedValue,
            filterOperator);
        return true;
    }

    private static bool TryNormalizeInput(
        string inputText,
        string selectedColumnName,
        out string normalized,
        out string error)
    {
        var trimmed = inputText.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            normalized = string.Empty;
            error = "Value cannot be empty.";
            return false;
        }

        if (selectedColumnName is nameof(AudioModel.BPM) or nameof(AudioModel.Bitrate))
        {
            if (!short.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                normalized = string.Empty;
                error = $"{selectedColumnName} must be a whole number.";
                return false;
            }

            normalized = number.ToString(CultureInfo.InvariantCulture);
            error = string.Empty;
            return true;
        }

        if (selectedColumnName == nameof(AudioModel.Length))
        {
            if (!TryParseLength(trimmed, out var length))
            {
                normalized = string.Empty;
                error = "Length must be mm:ss or total seconds.";
                return false;
            }

            normalized = length.ToString("c", CultureInfo.InvariantCulture);
            error = string.Empty;
            return true;
        }

        normalized = trimmed;
        error = string.Empty;
        return true;
    }

    private static bool TryParseLength(string value, out TimeSpan length)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
        {
            length = TimeSpan.FromSeconds(seconds);
            return true;
        }

        if (TimeSpan.TryParseExact(
                value,
                ["m\\:ss", "mm\\:ss", "h\\:mm\\:ss", "hh\\:mm\\:ss", "c"],
                CultureInfo.InvariantCulture,
                out var parsed) &&
            parsed >= TimeSpan.Zero)
        {
            length = parsed;
            return true;
        }

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out parsed) && parsed >= TimeSpan.Zero)
        {
            length = parsed;
            return true;
        }

        length = TimeSpan.Zero;
        return false;
    }

    private bool CanAddCriteria()
    {
        if (string.IsNullOrWhiteSpace(SelectedColumnName) ||
            string.IsNullOrWhiteSpace(SelectedRelation) ||
            string.IsNullOrWhiteSpace(InputText))
        {
            return false;
        }

        return TryNormalizeInput(InputText, SelectedColumnName, out _, out _);
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
        FontFamily = notification.FontFamily;
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
