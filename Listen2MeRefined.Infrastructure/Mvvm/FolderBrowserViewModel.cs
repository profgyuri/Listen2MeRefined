namespace Listen2MeRefined.Infrastructure.Mvvm;
using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using Listen2MeRefined.Infrastructure.SystemOperations;
using MediatR;

public sealed partial class FolderBrowserViewModel : 
    ObservableObject,
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

    public FolderBrowserViewModel(
        ILogger logger,
        IFolderBrowser folderBrowser,
        IMediator mediator,
        ISettingsManager<AppSettings> settingsManager)
    {
        _logger = logger;
        _folderBrowser = folderBrowser;
        _mediator = mediator;
        _settingsManager = settingsManager;
        
        Initialize().ConfigureAwait(false);
    }

    private async Task Initialize()
    {
        await Task.Run(() =>
        {
            FontFamily = _settingsManager.Settings.FontFamily;

            ChangeDirectory();
        });
    }

    [RelayCommand]
    private void ChangeDirectory()
    {
        FullPath = SelectedFolder switch
        {
            GlobalConstants.ParentPathItem => Path.GetDirectoryName(FullPath) ?? "",
            _ => Path.Combine(FullPath, SelectedFolder)
        };

        Folders.Clear();

        if (string.IsNullOrEmpty(FullPath))
        {
            GetDriveNames();
        }
        else
        {
            GetFolderNames();
        }

        SelectedFolder = "";
    }

    /// <summary>
    ///     This method should be called when the browsing is done,
    ///     and we have a selected path.
    /// </summary>
    [RelayCommand]
    private async Task HandleSelectedPath()
    {
        var hasSelectedChildPath =
            !string.IsNullOrEmpty(SelectedFolder) && SelectedFolder != GlobalConstants.ParentPathItem;
        if (hasSelectedChildPath)
        {
            FullPath = Path.Combine(FullPath, SelectedFolder);
        }

        var isFullPathInvalid = string.IsNullOrEmpty(FullPath) || !new DirectoryInfo(FullPath).Exists;
        if (isFullPathInvalid)
        {
            return;
        }

        _logger.Debug("Publishing {FullPath} from the folder browser dialog", FullPath);

        var notification = new FolderBrowserNotification(FullPath);
        await _mediator.Publish(notification);
    }

    private void GetDriveNames()
    {
        foreach (var drive in _folderBrowser.GetDrives())
        {
            Folders.Add(drive);
        }
    }

    private void GetFolderNames()
    {
        Folders.Add(GlobalConstants.ParentPathItem);

        foreach (var folder in _folderBrowser.GetSubFolders(FullPath))
        {
            Folders.Add(folder);
        }
    }

    #region Implementation of INotificationHandler<in FontFamilyChangedNotification>
    /// <inheritdoc />
    public async Task Handle(
        FontFamilyChangedNotification notification,
        CancellationToken cancellationToken)
    {
        FontFamily = notification.FontFamily;
        await Task.CompletedTask;
    }
    #endregion
}