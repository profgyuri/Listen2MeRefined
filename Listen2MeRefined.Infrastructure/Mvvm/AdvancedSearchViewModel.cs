using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public partial class AdvancedSearchViewModel : 
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly List<string> _numericRelations = new() { "Is", "Is not", "Bigger than", "Less than" };
    private readonly List<string> _timeRelations = new() { "Is", "Is not", "More than", "Less than" };
    private readonly List<string> _stringRelations = new() { "Is", "Is not", "Contains", "Does not contain" };
    private readonly List<AdvancedFilter> _filters = new();

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private List<string> _columnName;
    [ObservableProperty] private List<string> _relation;
    [ObservableProperty] private string _selectedRelation = "";
    [ObservableProperty] private ObservableCollection<string> _criterias;
    [ObservableProperty] private string _selectedCriteria;
    [ObservableProperty] private string _rangeSuffixText = "";
    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private bool _matchAll;
    
    private string _selectedColumnName;

    public string SelectedColumnName
    {
        get => _selectedColumnName;
        set
        {
            _selectedColumnName = value;
            switch (value)
            {
                case nameof(AudioModel.Length):
                    Relation = _timeRelations;
                    RangeSuffixText = "seconds";
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

            OnPropertyChanged();
        }
    }

    public AdvancedSearchViewModel(
        IMediator mediator,
        ILogger logger,
        ISettingsManager<AppSettings> settingsManager)
    {
        _mediator = mediator;
        _logger = logger;
        _settingsManager = settingsManager;

        _logger.Debug("[AdvancedSearchViewModel] initialized");
    }

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        await Task.Run(() =>
        {
            FontFamily = _settingsManager.Settings.FontFamily;
            Criterias = new();
            ColumnName = GetAudioModelProperties();
            Relation = _stringRelations;
            SelectedColumnName = ColumnName.First();
            SelectedRelation = Relation.First();
        }, ct);

        _logger.Debug("[AdvancedSearchViewModel] Finished InitializeCoreAsync");
    }

    [RelayCommand]
    private void AddCriteria()
    {
        if (string.IsNullOrEmpty(SelectedColumnName) ||
            string.IsNullOrEmpty(SelectedRelation) ||
            string.IsNullOrEmpty(InputText))
        {
            _logger.Warning("[AdvancedSearchViewModel] Cannot add criteria. One or more fields are empty.");
            return;
        }

        var filter = new AdvancedFilter(SelectedColumnName, MapOperator(SelectedRelation), InputText);
        _filters.Add(filter);
        Criterias.Add($"{SelectedColumnName} {SelectedRelation} {InputText}");

        _logger.Debug("[AdvancedSearchViewModel] Added criteria: {@Filter}", filter);
        InputText = "";
    }

    [RelayCommand]
    private void DeleteItem()
    {
        if (string.IsNullOrEmpty(SelectedCriteria) || !Criterias.Contains(SelectedCriteria))
        {
            return;
        }
        
        var index = Criterias.IndexOf(SelectedCriteria);
        Criterias.RemoveAt(index);
        _filters.RemoveAt(index);
        _logger.Debug("[AdvancedSearchViewModel] Deleted criteria: {Criteria}", SelectedCriteria);
    }
    
    public async Task SearchAsync()
    {
        if (_filters.Count == 0)
        {
            return;
        }
        
        _logger.Information("Starting advanced search with these filters: {@Filters}", _filters);
        await _mediator.Publish(new AdvancedSearchNotification(_filters.ToList(), MatchAll));
        _filters.Clear();
        Criterias.Clear();
        
        _logger.Information("[AdvancedSearchViewModel] Search executed and criteria cleared.");
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

    public Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        _logger.Information("[AdvancedSearchViewModel] Received FontFamilyChangedNotification: {FontFamily}", notification.FontFamily);
        FontFamily = notification.FontFamily;
        return Task.CompletedTask;
    }
}