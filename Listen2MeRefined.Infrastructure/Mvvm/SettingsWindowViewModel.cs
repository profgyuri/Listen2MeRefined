namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System.Collections.ObjectModel;

[INotifyPropertyChanged]
public partial class SettingsWindowViewModel : INotificationHandler<FolderBrowserNotification>
{
    private readonly ILogger _logger;

    [ObservableProperty] private string _fontFamily = "Helvetica";
    [ObservableProperty] private string? _selectedFolder;
    [ObservableProperty] private ObservableCollection<string> _folders = new();

    public SettingsWindowViewModel(ILogger logger)
    {
        _logger = logger;
    }

    [ICommand]
    private void RemoveFolder()
    {
        Folders.Remove(SelectedFolder!);
    }

    #region Notification Handlers
    public Task Handle(FolderBrowserNotification notification, CancellationToken cancellationToken)
    {
        _logger.Debug("Folder received in settings window from folder browser: {0}", notification.Path);
        
        _folders.Add(notification.Path);
        return Task.CompletedTask;
    }
    #endregion
}