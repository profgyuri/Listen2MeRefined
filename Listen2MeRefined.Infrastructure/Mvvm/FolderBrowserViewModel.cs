using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Notifications;
using Listen2MeRefined.Infrastructure.Storage;
using Listen2MeRefined.Infrastructure.SystemOperations;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed partial class FolderBrowserViewModel :
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IFolderBrowser _folderBrowser;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly List<string> _allFolders = new();

    [ObservableProperty] private string _fontFamily = "";
    [ObservableProperty] private string _fullPath = "";
    [ObservableProperty] private string _selectedFolder = "";
    [ObservableProperty] private ObservableCollection<string> _folders = new();
    [ObservableProperty] private ObservableCollection<string> _pinnedFolders = new();
    [ObservableProperty] private ObservableCollection<string> _drives = new();
    [ObservableProperty] private string _selectedPinnedFolder = "";
    [ObservableProperty] private string _selectedDrive = "";
    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private string _validationMessage = "";
    [ObservableProperty] private bool _hasValidationError;

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

        _logger.Debug("[FolderBrowserViewModel] initialized");
    }

    protected override Task InitializeCoreAsync(CancellationToken ct)
    {
        FontFamily = _settingsManager.Settings.FontFamily;
        LoadQuickAccessCollections();

        var initialPath = GetInitialPath();
        if (string.IsNullOrWhiteSpace(initialPath))
        {
            LoadDrivesView();
        }
        else
        {
            NavigateToPathInternal(initialPath);
        }

        _logger.Debug("[FolderBrowserViewModel] Finished InitializeCoreAsync");
        return Task.CompletedTask;
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private void NavigateIntoSelected()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolder))
        {
            return;
        }

        if (SelectedFolder == GlobalConstants.ParentPathItem)
        {
            NavigateParent();
            return;
        }

        var newPath = string.IsNullOrWhiteSpace(FullPath)
            ? SelectedFolder
            : Path.Combine(FullPath, SelectedFolder);
        NavigateToPathInternal(newPath);
    }

    [RelayCommand]
    private void NavigateParent()
    {
        if (string.IsNullOrWhiteSpace(FullPath))
        {
            LoadDrivesView();
            return;
        }

        var parentPath = _folderBrowser.GetParent(FullPath);
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            LoadDrivesView();
            return;
        }

        NavigateToPathInternal(parentPath);
    }

    [RelayCommand]
    private void GoToPath()
    {
        if (!TryNavigateToPath(FullPath))
        {
            SetValidationError($"Could not open '{FullPath}'.");
        }
    }

    [RelayCommand]
    private void SelectPinnedFolder()
    {
        SelectQuickAccessPath(SelectedPinnedFolder);
    }

    [RelayCommand]
    private void SelectDrive()
    {
        SelectQuickAccessPath(SelectedDrive);
    }

    [RelayCommand]
    private void SelectQuickAccessPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!TryNavigateToPath(path))
        {
            SetValidationError($"Could not open '{path}'.");
        }
    }

    [RelayCommand]
    private async Task TogglePinAsync()
    {
        if (string.IsNullOrWhiteSpace(FullPath) || !_folderBrowser.DirectoryExists(FullPath))
        {
            return;
        }

        if (PinnedFolders.Contains(FullPath))
        {
            PinnedFolders.Remove(FullPath);
        }
        else
        {
            PinnedFolders.Insert(0, FullPath);
        }

        await SavePinnedFoldersAsync();
    }

    [RelayCommand]
    private async Task HandleSelectedPathAsync()
    {
        await TryHandleSelectedPathAsync();
    }

    public async Task<bool> TryHandleSelectedPathAsync()
    {
        var candidatePath = FullPath;
        var hasSelectedChildPath = !string.IsNullOrWhiteSpace(SelectedFolder) &&
                                   SelectedFolder != GlobalConstants.ParentPathItem;
        if (hasSelectedChildPath)
        {
            candidatePath = string.IsNullOrWhiteSpace(FullPath)
                ? SelectedFolder
                : Path.Combine(FullPath, SelectedFolder);
        }

        var isFullPathInvalid = string.IsNullOrWhiteSpace(candidatePath) || !_folderBrowser.DirectoryExists(candidatePath);
        if (isFullPathInvalid)
        {
            SetValidationError("Please select a valid folder before confirming.");
            _logger.Warning("[FolderBrowserViewModel] Full path is invalid or does not exist: {FullPath}", candidatePath);
            return false;
        }

        ClearValidationError();
        FullPath = candidatePath;
        _settingsManager.SaveSettings(s => s.LastBrowsedFolder = candidatePath);

        _logger.Debug("[FolderBrowserViewModel] Publishing full path: {FullPath}", candidatePath);
        var notification = new FolderBrowserNotification(candidatePath);
        await _mediator.Publish(notification);
        return true;
    }

    private string GetInitialPath()
    {
        var settings = _settingsManager.Settings;
        if (settings.FolderBrowserStartAtLastLocation &&
            _folderBrowser.DirectoryExists(settings.LastBrowsedFolder))
        {
            return settings.LastBrowsedFolder;
        }

        return settings.PinnedFolders.FirstOrDefault(_folderBrowser.DirectoryExists) ?? "";
    }

    private void LoadQuickAccessCollections()
    {
        var settings = _settingsManager.Settings;

        Drives = new(_folderBrowser.GetDrives().Distinct(StringComparer.OrdinalIgnoreCase));

        var pinnedFolders = settings.PinnedFolders
            .Where(_folderBrowser.DirectoryExists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!pinnedFolders.SequenceEqual(settings.PinnedFolders, StringComparer.OrdinalIgnoreCase))
        {
            _settingsManager.SaveSettings(x => x.PinnedFolders = pinnedFolders);
        }

        PinnedFolders = new(pinnedFolders);
    }

    private bool TryNavigateToPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            LoadDrivesView();
            return true;
        }

        if (!_folderBrowser.DirectoryExists(path))
        {
            return false;
        }

        NavigateToPathInternal(path);
        return true;
    }

    private void NavigateToPathInternal(string path)
    {
        ClearValidationError();
        FullPath = path;

        _logger.Information("[FolderBrowserViewModel] Changing directory to {FullPath}", FullPath);
        _settingsManager.SaveSettings(s => s.LastBrowsedFolder = path);

        _allFolders.Clear();
        _allFolders.Add(GlobalConstants.ParentPathItem);
        _allFolders.AddRange(_folderBrowser.GetSubFoldersSafe(path));
        ApplyFilter();

        SelectedFolder = "";
    }

    private void LoadDrivesView()
    {
        ClearValidationError();
        FullPath = "";

        _allFolders.Clear();
        _allFolders.AddRange(Drives);
        ApplyFilter();

        SelectedFolder = "";
    }

    private void ApplyFilter()
    {
        var filter = FilterText?.Trim() ?? "";
        var filteredFolders = string.IsNullOrWhiteSpace(filter)
            ? _allFolders
            : _allFolders.Where(x => x.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        Folders.Clear();
        foreach (var folder in filteredFolders)
        {
            Folders.Add(folder);
        }
    }

    private async Task SavePinnedFoldersAsync()
    {
        var pinnedFolders = PinnedFolders
            .Where(_folderBrowser.DirectoryExists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _settingsManager.SaveSettings(s => s.PinnedFolders = pinnedFolders);
        await _mediator.Publish(new PinnedFoldersChangedNotification(pinnedFolders));
    }

    private void SetValidationError(string message)
    {
        ValidationMessage = message;
        HasValidationError = true;
    }

    private void ClearValidationError()
    {
        ValidationMessage = "";
        HasValidationError = false;
    }

    public async Task Handle(
        FontFamilyChangedNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.Information("[FolderBrowserViewModel] Received FontFamilyChangedNotification: {FontFamily}", notification.FontFamily);
        FontFamily = notification.FontFamily;
        await Task.CompletedTask;
    }
}
