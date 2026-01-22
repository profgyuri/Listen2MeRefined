namespace Listen2MeRefined.Infrastructure.Mvvm;
using System.Collections.ObjectModel;
using System.Text;
using Dapper;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using MediatR;

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
    private readonly List<ParameterizedQuery> _queryStatements = new();

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private List<string> _columnName;
    private string _selectedColumnName;
    [ObservableProperty] private List<string> _relation;
    [ObservableProperty] private string _selectedRelation = "";
    [ObservableProperty] private ObservableCollection<string> _criterias;
    [ObservableProperty] private string _selectedCriteria;
    [ObservableProperty] private string _rangeSuffixText = "";
    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private bool _matchAll;

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
    }

    [RelayCommand]
    private void AddCriteria()
    {
        if (string.IsNullOrEmpty(SelectedColumnName) ||
            string.IsNullOrEmpty(SelectedRelation) ||
            string.IsNullOrEmpty(InputText))
        {
            return;
        }
        
        Criterias.Add($"{SelectedColumnName} {SelectedRelation} {InputText}");
    
        var queryBuilder = new StringBuilder(SelectedColumnName);
        var relation = SelectedRelation switch
        {
            "Is" => " = ",
            "Is not" => " <> ",
            "Contains" => " LIKE ",
            "Does not contain" => " NOT LIKE ",
            "Bigger than" => " > ",
            "Less than" => " < ",
            "More than" => " > ",
            _ => throw new IndexOutOfRangeException($"This relation is not handled: {SelectedRelation}")
        };
        queryBuilder.Append(relation);
    
        var param = new DynamicParameters();
        // Fix: Check for both Contains and Does not contain with lowercase
        if (SelectedRelation is "Contains" or "Does not contain")
        {
            param.Add($"param{_queryStatements.Count}", $"%{InputText}%");
        }
        else
        {
            param.Add($"param{_queryStatements.Count}", $"{InputText}");
        }
    
        queryBuilder.Append($"@param{_queryStatements.Count}");
        
        _queryStatements.Add(new ParameterizedQuery(queryBuilder.ToString(), param));
        
        InputText = "";
    }

    [RelayCommand]
    private void DeleteItem()
    {
        if (!string.IsNullOrEmpty(SelectedCriteria) && Criterias.Contains(SelectedCriteria))
        {
            Criterias.Remove(SelectedCriteria);
        }
    }
    
    public void Search()
    {
        if (_queryStatements.Count == 0)
        {
            return;
        }
        
        _logger.Debug($"Starting advanced search with these filters:\n{Criterias}", string.Join('\n', Criterias));
        _mediator.Publish(new AdvancedSearchNotification(_queryStatements, MatchAll));
        _queryStatements.Clear();
        Criterias.Clear();
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

    #region Implementation of INotificationHandler<in FontFamilyChangedNotification>
    /// <inheritdoc />
    Task INotificationHandler<FontFamilyChangedNotification>.Handle(
        FontFamilyChangedNotification notification,
        CancellationToken cancellationToken)
    {
        FontFamily = notification.FontFamily;
        return Task.CompletedTask;
    }
    #endregion
}