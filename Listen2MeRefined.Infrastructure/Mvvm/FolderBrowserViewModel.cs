namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

[INotifyPropertyChanged]
public partial class FolderBrowserViewModel
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IFolderBrowser _folderBrowser;

    [ObservableProperty] private string _fullPath = "";
    [ObservableProperty] private string _selectedFolder = "";
    [ObservableProperty] private ObservableCollection<string> _folders = new();

    public FolderBrowserViewModel(ILogger logger, IFolderBrowser folderBrowser, IMediator mediator)
    {
        _logger = logger;
        _folderBrowser = folderBrowser;
        _mediator = mediator;
    }

    [ICommand]
    private void ChangeDirectory()
    {
        _fullPath = _selectedFolder switch
        {
            ".." => Path.GetDirectoryName(_fullPath) ?? "",
            _ => Path.Combine(_fullPath, _selectedFolder),
        };

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
    [ICommand]
    private async Task HandleSelectedPath()
    {
        if (!string.IsNullOrEmpty(_selectedFolder))
        {
            _fullPath = Path.Combine(_fullPath, _selectedFolder);
        }

        _logger.Debug("Publishing {0} from the folder browser dialog.", _fullPath);

        var notification = new FolderBrowserNotification(_fullPath);
        await _mediator.Publish(notification);
    }

    private void GetDriveNames()
    {
        _folders.Clear();

        foreach (var drive in _folderBrowser.GetDrives())
        {
            _folders.Add(drive);
        }
    }

    private void GetFolderNames()
    {
        _folders.Clear();

        foreach (var folder in _folderBrowser.GetSubFolders(_fullPath))
        {
            _folders.Add(folder);
        }
    }
}
