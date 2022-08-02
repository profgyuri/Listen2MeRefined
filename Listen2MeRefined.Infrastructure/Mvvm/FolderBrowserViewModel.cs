namespace Listen2MeRefined.Infrastructure.Mvvm;

using System.Collections.ObjectModel;

[INotifyPropertyChanged]
public partial class FolderBrowserViewModel
{
    private readonly ILogger _logger;
    private readonly IFolderBrowser _folderBrowser;

    [ObservableProperty] private string _fullPath = "";
    [ObservableProperty] private string _selectedFolder = "";
    [ObservableProperty] private ObservableCollection<string> _folders = new();

    public FolderBrowserViewModel(ILogger logger, IFolderBrowser folderBrowser)
    {
        _logger = logger;
        _folderBrowser = folderBrowser;
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
    private void HandleSelectedPath()
    {
        
    }
}
