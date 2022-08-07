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

        ChangeDirectory();
    }

    [RelayCommand]
    private void ChangeDirectory()
    {
        FullPath = _selectedFolder switch
        {
            ".." => Path.GetDirectoryName(_fullPath) ?? "",
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
        if (!string.IsNullOrEmpty(_selectedFolder) && _selectedFolder != "..")
        {
            FullPath = Path.Combine(_fullPath, _selectedFolder);
        }

        if (!File.Exists(_fullPath))
        {
            return;
        }

        _logger.Debug("Publishing {0} from the folder browser dialog.", _fullPath);

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
        _folders.Add("..");

        foreach (var folder in _folderBrowser.GetSubFolders(_fullPath))
        {
            _folders.Add(folder);
        }
    }
}
