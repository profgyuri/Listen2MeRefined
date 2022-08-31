using System.Windows.Media;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class AdvancedSearchViewModel :
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly ISettingsManager _settingsManager;
    
    [ObservableProperty] private FontFamily _fontFamily;

    public AdvancedSearchViewModel(IMediator mediator, ILogger logger, ISettingsManager settingsManager)
    {
        _mediator = mediator;
        _logger = logger;
        _settingsManager = settingsManager;
        _fontFamily = new FontFamily(_settingsManager.Settings.FontFamily);
    }
    
    #region Implementation of INotificationHandler<in FontFamilyChangedNotification>
    /// <inheritdoc />
    Task INotificationHandler<FontFamilyChangedNotification>.Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        FontFamily = notification.FontFamily;
        return Task.CompletedTask;
    }
    #endregion
}