using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Notifications;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Listen2MeRefined.Core.Enums;
using MediatR;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;

public partial class AdvancedSearchShellDefaultHomeViewModel : 
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly IUiDispatcher _ui;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAdvancedSearchCriteriaService _criteriaService;
    
    [ObservableProperty] private string _fontFamilyName = string.Empty;
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
                SelectedRelation = Relation.First();
            }

            OnPropertyChanged();
            _ui.InvokeAsync(() => AddCriteriaCommand.NotifyCanExecuteChanged());
        }
    } = string.Empty;

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
    
    public AdvancedSearchShellDefaultHomeViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        IUiDispatcher ui, 
        IAdvancedSearchCriteriaService criteriaService, 
        IAppSettingsReader settingsReader) : base(errorHandler, logger, messenger)
    {
        _ui = ui;
        _criteriaService = criteriaService;
        _settingsReader = settingsReader;
    }
    
    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        await _ui.InvokeAsync(() =>
        {
            Criterias.Clear();
            FontFamilyName = _settingsReader.GetFontFamily();
            ColumnName = _criteriaService.GetColumnNames().ToList();
            SelectedColumnName = ColumnName.FirstOrDefault() ?? string.Empty;
            MatchMode = SearchMatchMode.All;
            ValidationMessage = string.Empty;
            SearchStatusMessage = "Add at least one filter to search.";
            LastSearchResultCount = 0;
            HasSearchResults = false;
        }, ct);
        RegisterMessage<AdvancedSearchCompletedMessage>(OnAdvancedSearchCompletedMessage);

        Logger.Debug("[AdvancedSearchViewModel] Finished InitializeCoreAsync");
    }
    
    [RelayCommand(CanExecute = nameof(CanAddCriteria))]
    private async Task AddCriteria()
    {
        await ExecuteSafeAsync(_ =>
        {
            var result = _criteriaService.BuildCriterion(SelectedColumnName, SelectedRelation, InputText);
            if (!result.Success || result.Criterion is null)
            {
                ValidationMessage = result.ErrorMessage;
                Logger.Warning("[AdvancedSearchViewModel] Cannot add criteria. {Error}", result.ErrorMessage);
                return Task.CompletedTask;
            }

            Criterias.Add(result.Criterion);
            SelectedCriteria = result.Criterion;
            ValidationMessage = string.Empty;
            SearchStatusMessage = $"{Criterias.Count} filter(s) ready.";
            InputText = string.Empty;

            Logger.Debug("[AdvancedSearchViewModel] Added criteria: {@Filter}", result.Criterion);
            return Task.CompletedTask;
        });
    }
    
    [RelayCommand(CanExecute = nameof(CanDeleteItem))]
    private async Task DeleteItem(AdvancedSearchCriterion? criterion = null)
    {
        await ExecuteSafeAsync(async ct =>
        {
            var target = criterion ?? SelectedCriteria;
            if (target is null)
            {
                return;
            }

            await _ui.InvokeAsync(() => Criterias.Remove(target), ct);
            if (ReferenceEquals(SelectedCriteria, target))
            {
                SelectedCriteria = null;
            }

            await _ui.InvokeAsync(() =>
            {
                ValidationMessage = string.Empty;
                SearchStatusMessage = Criterias.Count == 0
                    ? "Add at least one filter to search."
                    : $"{Criterias.Count} filter(s) ready.";
            }, ct);
            Logger.Debug("[AdvancedSearchViewModel] Deleted criteria: {Criteria}", target.Display);
        });
    }
    
    [RelayCommand(CanExecute = nameof(CanEditCriteria))]
    private async Task EditCriteria(AdvancedSearchCriterion? criterion = null)
    {
        await ExecuteSafeAsync(async ct =>
        {
            var target = criterion ?? SelectedCriteria;
            if (target is null)
            {
                return;
            }

            SelectedColumnName = target.Field;
            SelectedRelation = target.Relation;
            SelectedCriteria = null;
            await _ui.InvokeAsync(() =>
            {
                InputText = target.RawValue;
                Criterias.Remove(target);
                ValidationMessage = "Editing filter. Press + or Enter to apply.";
                SearchStatusMessage = Criterias.Count == 0
                    ? "Add at least one filter to search."
                    : $"{Criterias.Count} filter(s) ready.";
            }, ct);
        });
    }
    
    [RelayCommand(CanExecute = nameof(CanDuplicateCriteria))]
    private async Task DuplicateCriteria(AdvancedSearchCriterion? criterion = null)
    {
        await ExecuteSafeAsync(async ct =>
        {
            var target = criterion ?? SelectedCriteria;
            if (target is null)
            {
                return;
            }

            var copy = target with { };
            await _ui.InvokeAsync(() =>
            {
                Criterias.Add(copy);
                SelectedCriteria = copy;
                ValidationMessage = string.Empty;
                SearchStatusMessage = $"{Criterias.Count} filter(s) ready.";
            }, ct);
        });
    }
    
    [RelayCommand(CanExecute = nameof(CanClearAll))]
    private async Task ClearAll()
    {
        await ExecuteSafeAsync(async ct =>
        {
            await _ui.InvokeAsync(() =>
            {
                Criterias.Clear();
                ValidationMessage = string.Empty;
                SearchStatusMessage = "Filters cleared.";
            }, ct);
            SelectedCriteria = null;
            LastSearchResultCount = 0;
            HasSearchResults = false;
        });
    }
    
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task Search()
    {
        await ExecuteSafeAsync(async ct =>
        {
            if (Criterias.Count == 0)
            {
                await _ui.InvokeAsync(() => ValidationMessage = "Add at least one filter before searching.", ct);
                return;
            }

            await _ui.InvokeAsync(() =>
            {
                ValidationMessage = string.Empty;
                SearchStatusMessage = "Searching...";
            }, ct);
            var filters = _criteriaService.BuildFilters(Criterias).ToList();

            Logger.Information("Starting advanced search with these filters: {@Filters}", filters);
            Messenger.Send(new AdvancedSearchRequestedMessage(new AdvancedSearchRequestedMessageData(filters, MatchMode)));

            await _ui.InvokeAsync(() =>
            {
                if (SearchStatusMessage == "Searching...")
                {
                    SearchStatusMessage = "Search completed. Check Search Results.";
                }
            }, ct);
        });
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

    partial void OnCriteriasChanged(ObservableCollection<AdvancedSearchCriterion>? oldValue, ObservableCollection<AdvancedSearchCriterion> newValue)
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
        Logger.Information("[AdvancedSearchViewModel] Received FontFamilyChangedNotification: {FontFamily}", notification.FontFamily);
        FontFamilyName = notification.FontFamily;
        return Task.CompletedTask;
    }

    private void OnAdvancedSearchCompletedMessage(AdvancedSearchCompletedMessage message)
    {
        LastSearchResultCount = message.Value;
        HasSearchResults = message.Value > 0;
        SearchStatusMessage = message.Value > 0
            ? $"Found {message.Value} result(s)."
            : "No matches found. Adjust filters and try again.";
    }
}
