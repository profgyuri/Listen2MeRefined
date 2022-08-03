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
        
    }

    /// <summary>
    ///     This method should be called when the browsing is done, 
    ///     and we have a selected path.
    /// </summary>
    [ICommand]
    private async Task HandleSelectedPath()
    {
        await _mediator.Publish(new FolderBrowserNotification(SelectedFolder));
    }
}
