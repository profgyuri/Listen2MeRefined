using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Utils;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;

public sealed partial class FolderBrowserShellDefaultHomeViewModel : ViewModelBase
{
    private readonly IFolderNavigationService _folderNavigationService;
    private readonly IPinnedFoldersService _pinnedFoldersService;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IClipboardService _clipboardService;
    private readonly List<string> _allFolders = [];

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private string _fullPath = string.Empty;
    [ObservableProperty] private string _selectedFolder = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _folders = [];
    [ObservableProperty] private ObservableCollection<string> _pinnedFolders = [];
    [ObservableProperty] private ObservableCollection<string> _drives = [];
    [ObservableProperty] private string _selectedPinnedFolder = string.Empty;
    [ObservableProperty] private string _selectedDrive = string.Empty;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private string _validationMessage = string.Empty;
    [ObservableProperty] private bool _hasValidationError;

    public FolderBrowserShellDefaultHomeViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IFolderNavigationService folderNavigationService,
        IPinnedFoldersService pinnedFoldersService,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IClipboardService clipboardService) : base(errorHandler, logger, messenger)
    {
        _folderNavigationService = folderNavigationService;
        _pinnedFoldersService = pinnedFoldersService;
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _clipboardService = clipboardService;
    }

    public override Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);

        FontFamilyName = _settingsReader.GetFontFamily();
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

        Logger.Debug("[FolderBrowserShellDefaultHomeViewModel] Initialized.");
        return Task.CompletedTask;
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private Task NavigateIntoSelected() =>
        ExecuteSafeAsync(_ =>
        {
            if (string.IsNullOrWhiteSpace(SelectedFolder))
            {
                return Task.CompletedTask;
            }

            if (SelectedFolder == GlobalConstants.ParentPathItem)
            {
                var parentResult = _folderNavigationService.NavigateParent(FullPath);
                if (!parentResult.Success)
                {
                    SetValidationError(parentResult.ErrorMessage);
                    return Task.CompletedTask;
                }

                ApplyNavigationResult(parentResult);
                return Task.CompletedTask;
            }

            var newPath = _folderNavigationService.BuildChildPath(FullPath, SelectedFolder);
            NavigateToPathInternal(newPath);
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task NavigateParent() =>
        ExecuteSafeAsync(_ =>
        {
            var result = _folderNavigationService.NavigateParent(FullPath);
            if (!result.Success)
            {
                SetValidationError(result.ErrorMessage);
                return Task.CompletedTask;
            }

            ApplyNavigationResult(result);
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task GoToPath() =>
        ExecuteSafeAsync(_ =>
        {
            if (!TryNavigateToPath(FullPath))
            {
                SetValidationError($"Could not open '{FullPath}'.");
            }

            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task NavigateToClipboardPath() =>
        ExecuteSafeAsync(_ =>
        {
            var text = _clipboardService.GetText().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                SetValidationError("Clipboard doesn't contain a valid path.");
                return Task.CompletedTask;
            }

            if (text.StartsWith('"') && text.EndsWith('"') && text.Length >= 2)
            {
                text = text[1..^1].Trim();
            }

            var rootPath = Path.GetPathRoot(text);
            if (text.EndsWith('\\')
                && !string.IsNullOrWhiteSpace(rootPath)
                && !rootPath.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                text = text.TrimEnd('\\');
            }

            var resolvedPath = text;
            if (File.Exists(text))
            {
                resolvedPath = Path.GetDirectoryName(text) ?? text;
            }

            Logger.Debug(
                "[FolderBrowserShellDefaultHomeViewModel] Ctrl+V navigation to '{Path}'",
                resolvedPath);
            if (!TryNavigateToPath(resolvedPath))
            {
                SetValidationError($"Could not open '{resolvedPath}'.");
            }

            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task SelectPinnedFolder() =>
        ExecuteSafeAsync(_ =>
        {
            SelectQuickAccessPath(SelectedPinnedFolder);
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task SelectDrive() =>
        ExecuteSafeAsync(_ =>
        {
            SelectQuickAccessPath(SelectedDrive);
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task SelectQuickAccessPath(string path) =>
        ExecuteSafeAsync(_ =>
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Task.CompletedTask;
            }

            if (!TryNavigateToPath(path))
            {
                SetValidationError($"Could not open '{path}'.");
            }

            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task TogglePin() =>
        ExecuteSafeAsync(async _ =>
        {
            if (string.IsNullOrWhiteSpace(FullPath) || !_folderNavigationService.DirectoryExists(FullPath))
            {
                return;
            }

            var updatedPins = _pinnedFoldersService.TogglePinnedFolder(PinnedFolders, FullPath);
            PinnedFolders = new ObservableCollection<string>(updatedPins);
            await SavePinnedFoldersAsync();
        });

    [RelayCommand]
    private Task HandleSelectedPath() =>
        ExecuteSafeAsync(async ct =>
        {
            await TryHandleSelectedPathCoreAsync(ct);
        });

    public async Task<bool> TryHandleSelectedPathAsync(CancellationToken ct = default)
    {
        var handled = false;
        await ExecuteSafeAsync(async token =>
        {
            handled = await TryHandleSelectedPathCoreAsync(token);
        }, cancellationToken: ct);
        return handled;
    }

    private async Task<bool> TryHandleSelectedPathCoreAsync(CancellationToken ct = default)
    {
        var candidatePath = FullPath;
        var hasSelectedChildPath = !string.IsNullOrWhiteSpace(SelectedFolder)
                                   && SelectedFolder != GlobalConstants.ParentPathItem;
        if (hasSelectedChildPath)
        {
            candidatePath = _folderNavigationService.BuildChildPath(FullPath, SelectedFolder);
        }

        var isInvalidPath = string.IsNullOrWhiteSpace(candidatePath)
                            || !_folderNavigationService.DirectoryExists(candidatePath);
        if (isInvalidPath)
        {
            SetValidationError("Please select a valid folder before confirming.");
            Logger.Warning(
                "[FolderBrowserShellDefaultHomeViewModel] Full path is invalid or missing: {Path}",
                candidatePath);
            return false;
        }

        ClearValidationError();
        FullPath = candidatePath;
        _settingsWriter.SetLastBrowsedFolder(candidatePath);

        Logger.Debug(
            "[FolderBrowserShellDefaultHomeViewModel] Publishing selected path: {Path}",
            candidatePath);
        Messenger.Send(new FolderBrowserPathSelectedMessage(candidatePath));
        await Task.CompletedTask;
        return true;
    }

    private void LoadQuickAccessCollections()
    {
        Drives = new(_folderNavigationService.GetDrives());

        var sourcePinnedFolders = _settingsReader.GetPinnedFolders();
        var normalizedPins = _pinnedFoldersService.NormalizeExisting(sourcePinnedFolders);
        if (!normalizedPins.SequenceEqual(sourcePinnedFolders, StringComparer.OrdinalIgnoreCase))
        {
            _settingsWriter.SetPinnedFolders(normalizedPins);
        }

        PinnedFolders = new(normalizedPins);
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
            Logger.Information(
                "[FolderBrowserShellDefaultHomeViewModel] Changing directory to {FullPath}",
                FullPath);
            _settingsWriter.SetLastBrowsedFolder(FullPath);
        }

        _allFolders.Clear();
        _allFolders.AddRange(result.Entries);
        ApplyFilter();
        SelectedFolder = string.Empty;
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
        Messenger.Send(new PinnedFoldersChangedMessage(pinnedFolders));
        await Task.CompletedTask;
    }

    private void SetValidationError(string message)
    {
        ValidationMessage = message;
        HasValidationError = true;
    }

    private void ClearValidationError()
    {
        ValidationMessage = string.Empty;
        HasValidationError = false;
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        FontFamilyName = message.Value;
    }
}
