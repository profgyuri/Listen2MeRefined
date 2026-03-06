using System.Collections.ObjectModel;
using System.IO;
using Listen2MeRefined.Infrastructure.FolderBrowser;
using Listen2MeRefined.Infrastructure.Notifications;

namespace Listen2MeRefined.Infrastructure.ViewModels;

public sealed partial class FolderBrowserViewModel :
    ViewModelBase,
    INotificationHandler<FontFamilyChangedNotification>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IFolderNavigationService _folderNavigationService;
    private readonly IPinnedFoldersService _pinnedFoldersService;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IClipboardService _clipboardService;
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
        IMediator mediator,
        IFolderNavigationService folderNavigationService,
        IPinnedFoldersService pinnedFoldersService,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IClipboardService clipboardService)
    {
        _logger = logger;
        _mediator = mediator;
        _folderNavigationService = folderNavigationService;
        _pinnedFoldersService = pinnedFoldersService;
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _clipboardService = clipboardService;

        _logger.Debug("[FolderBrowserViewModel] initialized");
    }

    protected override Task InitializeCoreAsync(CancellationToken ct)
    {
        FontFamily = _settingsReader.GetFontFamily();
        LoadQuickAccessCollections();

        var initialPath = _folderNavigationService.ResolveInitialPath(
            _settingsReader.GetFolderBrowserStartAtLastLocation(),
            _settingsReader.GetLastBrowsedFolder(),
            PinnedFolders);
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

        var newPath = _folderNavigationService.BuildChildPath(FullPath, SelectedFolder);
        NavigateToPathInternal(newPath);
    }

    [RelayCommand]
    private void NavigateParent()
    {
        var result = _folderNavigationService.NavigateParent(FullPath);
        if (!result.Success)
        {
            SetValidationError(result.ErrorMessage);
            return;
        }

        ApplyNavigationResult(result);
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
    private void NavigateToClipboardPath()
    {
        var text = _clipboardService.GetText().Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            SetValidationError("Clipboard doesn't contain a valid path.");
            return;
        }

        // 'Copy as Path' wraps the path in double-quotes – strip them if present.
        if (text.StartsWith('"') && text.EndsWith('"') && text.Length >= 2)
        {
            text = text[1..^1].Trim();
        }

        // A trailing backslash confuses Path.GetDirectoryName – strip it unless it
        // is the root of a drive (e.g. "C:\").
        if (text.EndsWith('\\') && !Path.GetPathRoot(text).Equals(text, StringComparison.OrdinalIgnoreCase))
        {
            text = text.TrimEnd('\\');
        }

        // If the clipboard contains a file path, use its parent directory.
        var resolvedPath = text;
        if (File.Exists(text))
        {
            resolvedPath = Path.GetDirectoryName(text) ?? text;
        }

        _logger.Debug("[FolderBrowserViewModel] Ctrl+V navigation to '{Path}'", resolvedPath);

        if (!TryNavigateToPath(resolvedPath))
        {
            SetValidationError($"Could not open '{resolvedPath}'.");
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
        if (string.IsNullOrWhiteSpace(FullPath) || !_folderNavigationService.DirectoryExists(FullPath))
        {
            return;
        }

        var updatedPins = _pinnedFoldersService.TogglePinnedFolder(PinnedFolders, FullPath);
        PinnedFolders = new ObservableCollection<string>(updatedPins);

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
            candidatePath = _folderNavigationService.BuildChildPath(FullPath, SelectedFolder);
        }

        var isFullPathInvalid = string.IsNullOrWhiteSpace(candidatePath) || !_folderNavigationService.DirectoryExists(candidatePath);
        if (isFullPathInvalid)
        {
            SetValidationError("Please select a valid folder before confirming.");
            _logger.Warning<string>("[FolderBrowserViewModel] Full path is invalid or does not exist: {FullPath}", candidatePath);
            return false;
        }

        ClearValidationError();
        FullPath = candidatePath;
        _settingsWriter.SetLastBrowsedFolder(candidatePath);

        _logger.Debug<string>("[FolderBrowserViewModel] Publishing full path: {FullPath}", candidatePath);
        var notification = new FolderBrowserNotification(candidatePath);
        await _mediator.Publish(notification);
        return true;
    }

    private void LoadQuickAccessCollections()
    {
        Drives = new(_folderNavigationService.GetDrives());

        var sourcePinnedFolders = _settingsReader.GetPinnedFolders();
        var pinnedFolders = _pinnedFoldersService.NormalizeExisting(sourcePinnedFolders);

        if (!pinnedFolders.SequenceEqual(sourcePinnedFolders, StringComparer.OrdinalIgnoreCase))
        {
            _settingsWriter.SetPinnedFolders(pinnedFolders);
        }

        PinnedFolders = new(pinnedFolders);
    }

    private bool TryNavigateToPath(string path)
    {
        var result = _folderNavigationService.NavigateToPath(path);
        if (!result.Success)
        {
            return false;
        }

        ApplyNavigationResult(result);
        return true;
    }

    private void NavigateToPathInternal(string path)
    {
        var result = _folderNavigationService.NavigateToPath(path);
        if (!result.Success)
        {
            SetValidationError(result.ErrorMessage);
            return;
        }

        ApplyNavigationResult(result);
    }

    private void LoadDrivesView()
    {
        ApplyNavigationResult(_folderNavigationService.LoadDrivesView());
    }

    private void ApplyNavigationResult(FolderNavigationResult result)
    {
        ClearValidationError();
        FullPath = result.FullPath;

        if (!string.IsNullOrWhiteSpace(FullPath))
        {
            _logger.Information<string>("[FolderBrowserViewModel] Changing directory to {FullPath}", FullPath);
            _settingsWriter.SetLastBrowsedFolder(FullPath);
        }

        _allFolders.Clear();
        _allFolders.AddRange(result.Entries);
        ApplyFilter();

        SelectedFolder = "";
    }

    private void ApplyFilter()
    {
        var filteredFolders = _folderNavigationService.ApplyFilter(_allFolders, FilterText);

        Folders.Clear();
        foreach (var folder in filteredFolders)
        {
            Folders.Add(folder);
        }
    }

    private async Task SavePinnedFoldersAsync()
    {
        var pinnedFolders = _pinnedFoldersService.NormalizeExisting(PinnedFolders);
        _settingsWriter.SetPinnedFolders(pinnedFolders);
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
