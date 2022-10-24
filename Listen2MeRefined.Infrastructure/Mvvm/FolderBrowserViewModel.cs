﻿using Listen2MeRefined.Infrastructure.Data;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

[INotifyPropertyChanged]
public partial class FolderBrowserViewModel :
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IFolderBrowser _folderBrowser;
    private readonly ISettingsManager<AppSettings> _settingsManager;

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private string _fullPath = "";
    [ObservableProperty] private string _selectedFolder = "";
    [ObservableProperty] private ObservableCollection<string> _folders = new();

    public FolderBrowserViewModel(ILogger logger, IFolderBrowser folderBrowser, IMediator mediator,
        ISettingsManager<AppSettings> settingsManager)
    {
        _logger = logger;
        _folderBrowser = folderBrowser;
        _mediator = mediator;
        _settingsManager = settingsManager;
        _fontFamily = _settingsManager.Settings.FontFamily;

        ChangeDirectory();
    }

    [RelayCommand]
    private void ChangeDirectory()
    {
        FullPath = _selectedFolder switch
        {
            GlobalConstants.ParentPathItem => Path.GetDirectoryName(_fullPath) ?? "",
            _ => Path.Combine(_fullPath, _selectedFolder),
        };

        _folders.Clear();

        if (string.IsNullOrEmpty(_fullPath))
        {
            GetDriveNames();
        }
        else
        {
            GetFolderNames();
        }

        _selectedFolder = "";
    }

    /// <summary>
    ///     This method should be called when the browsing is done, 
    ///     and we have a selected path.
    /// </summary>
    [RelayCommand]
    private async Task HandleSelectedPath()
    {
        var hasSelectedChildPath = !string.IsNullOrEmpty(_selectedFolder) && _selectedFolder != GlobalConstants.ParentPathItem;
        if (hasSelectedChildPath)
        {
            FullPath = Path.Combine(_fullPath, _selectedFolder);
        }

        var isFullPathInvalid = string.IsNullOrEmpty(_fullPath) || !new DirectoryInfo(_fullPath).Exists;
        if (isFullPathInvalid)
        {
            return;
        }

        _logger.Debug("Publishing {FullPath} from the folder browser dialog", _fullPath);

        var notification = new FolderBrowserNotification(_fullPath);
        await _mediator.Publish(notification);
    }

    private void GetDriveNames()
    {
        foreach (var drive in _folderBrowser.GetDrives())
        {
            _folders.Add(drive);
        }
    }

    private void GetFolderNames()
    {
        _folders.Add(GlobalConstants.ParentPathItem);

        foreach (var folder in _folderBrowser.GetSubFolders(_fullPath))
        {
            _folders.Add(folder);
        }
    }

    #region Implementation of INotificationHandler<in FontFamilyChangedNotification>
    /// <inheritdoc />
    public async Task Handle(FontFamilyChangedNotification notification, CancellationToken cancellationToken)
    {
        FontFamily = notification.FontFamily;
        await Task.CompletedTask;
    }
    #endregion
}
