namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System;
using System.Collections.ObjectModel;

[INotifyPropertyChanged]
public partial class SettingsWindowViewModel : INotificationHandler<FolderBrowserNotification>
{
    private readonly ILogger _logger;
    private readonly ISettingsManager _settingsManager;

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private string? _selectedFolder;
    [ObservableProperty] private ObservableCollection<string> _folders;

    public SettingsWindowViewModel(ILogger logger, ISettingsManager settingsManager)
    {
        _logger = logger;
        _settingsManager = settingsManager;

        Init();
    }

    private void Init()
    {
        var settings = _settingsManager.Load();
        Folders = new(settings.MusicFolders);
        FontFamily = settings.FontFamily;
    }

    [RelayCommand]
    private void RemoveFolder()
    {
        _logger.Debug("Removing folder: {0}", SelectedFolder);

        Folders.Remove(SelectedFolder!);

        _settingsManager.Save(s => s.MusicFolders = _folders);
    }

    #region Notification Handlers
    public Task Handle(FolderBrowserNotification notification, CancellationToken cancellationToken)
    {
        _logger.Debug("Adding path to music folders: {0}", notification.Path);
        
        Folders.Add(notification.Path);

        _settingsManager.Save(s => s.MusicFolders = _folders);
        return Task.CompletedTask;
    }
    #endregion
}