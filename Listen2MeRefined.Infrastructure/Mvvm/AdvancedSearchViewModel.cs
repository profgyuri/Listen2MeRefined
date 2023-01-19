using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class AdvancedSearchViewModel : INotificationHandler<FontFamilyChangedNotification>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly List<string> _numericRelations = new() { "Is", "Is not", "Bigger than", "Less than" };
    private readonly List<string> _timeRelations = new() { "Is", "Is not", "Later than", "Earlier than" };
    private readonly List<string> _stringRelations = new() { "Is", "Is not", "Contains", "Does not contain" };

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private List<string> _columnName;
    private string _selectedColumnName;
    [ObservableProperty] private List<string> _relation;
    [ObservableProperty] private string _selectedRelation;
    [ObservableProperty] private ObservableCollection<string> _criterias;
    [ObservableProperty] private string _rangeSuffixText;
    [ObservableProperty] private string _inputText;

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
        _fontFamily = _settingsManager.Settings.FontFamily;
        _criterias = new ();
        _columnName = GetAudioModelProperties();
        _relation = _stringRelations;
        SelectedColumnName = _columnName.First();
        SelectedRelation = _relation.First();
    }

    [RelayCommand]
    private void AddCriteria()
    {
        _criterias.Add("New Criteria");
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