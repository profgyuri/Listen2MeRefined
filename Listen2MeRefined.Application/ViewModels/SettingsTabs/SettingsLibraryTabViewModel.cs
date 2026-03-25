using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Threading;
using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Core.Enums;
using Listen2MeRefined.Core.Repositories;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.SettingsTabs;

public partial class SettingsLibraryTabViewModel : ViewModelBase
{
    private const int MinTaskPercentageStep = 1;
    private const int MaxTaskPercentageStep = 25;
    private const int MinScanMilestoneInterval = 5;
    private const int MaxScanMilestoneInterval = 500;

    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IFromFolderRemover _fromFolderRemover;
    private readonly IFolderScanner _folderScanner;
    private readonly IPinnedFoldersService _pinnedFoldersService;
    private readonly IBackgroundTaskStatusService _backgroundTaskStatusService;
    private readonly IWindowManager _windowManager;
    private bool _isLoadingSettings;
    private bool _isUpdatingFolderSelection;
    private readonly Dictionary<string, bool> _folderRecursionByPath = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private string? _selectedFolder = string.Empty;
    [ObservableProperty] private bool _selectedFolderIncludeSubdirectories;
    [ObservableProperty] private ObservableCollection<string> _folders = [];
    [ObservableProperty] private bool _autoScanOnFolderAdd = true;
    [ObservableProperty] private bool _showTaskPercentage = true;
    [ObservableProperty] private int _taskPercentageReportInterval = 1;
    [ObservableProperty] private bool _showScanMilestoneCount;
    [ObservableProperty] private int _scanMilestoneInterval = 25;
    [ObservableProperty] private TaskStatusCountBasis _selectedScanMilestoneBasis = TaskStatusCountBasis.Processed;
    [ObservableProperty] private bool _folderBrowserStartAtLastLocation = true;
    [ObservableProperty] private ObservableCollection<string> _pinnedFolders = [];
    [ObservableProperty] private string? _selectedPinnedFolder = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _mutedDroppedSongFolders = [];
    [ObservableProperty] private string? _selectedMutedDroppedSongFolder = string.Empty;

    public ObservableCollection<TaskStatusCountBasis> ScanMilestoneBases { get; } =
        new(Enum.GetValues<TaskStatusCountBasis>());

    public bool ScanOnStartup
    {
        get => _settingsReader.GetScanOnStartup();
        set
        {
            _settingsWriter.SetScanOnStartup(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(DontScanOnStartup));
        }
    }

    public bool DontScanOnStartup => !ScanOnStartup;

    public SettingsLibraryTabViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IFromFolderRemover fromFolderRemover,
        IFolderScanner folderScanner,
        IPinnedFoldersService pinnedFoldersService,
        IBackgroundTaskStatusService backgroundTaskStatusService,
        IWindowManager windowManager) : base(errorHandler, logger, messenger)
    {
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _fromFolderRemover = fromFolderRemover;
        _folderScanner = folderScanner;
        _pinnedFoldersService = pinnedFoldersService;
        _backgroundTaskStatusService = backgroundTaskStatusService;
        _windowManager = windowManager;
    }

    public override Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);
        RegisterMessage<FolderBrowserPathSelectedMessage>(OnFolderBrowserPathSelectedMessage);
        RegisterMessage<PinnedFoldersChangedMessage>(OnPinnedFoldersChangedMessage);
        
        _isLoadingSettings = true;
        try
        {
            FontFamilyName = _settingsReader.GetFontFamily();
            var musicFolderRequests = _settingsReader.GetMusicFolderRequests();
            _folderRecursionByPath.Clear();
            foreach (var folderRequest in musicFolderRequests)
            {
                _folderRecursionByPath[folderRequest.Path] = folderRequest.IncludeSubdirectories;
            }

            Folders = new ObservableCollection<string>(musicFolderRequests.Select(x => x.Path));
            SelectedFolder = Folders.FirstOrDefault();
            AutoScanOnFolderAdd = _settingsReader.GetAutoScanOnFolderAdd();
            ShowTaskPercentage = _settingsReader.GetShowTaskPercentage();
            TaskPercentageReportInterval = Math.Clamp(
                (int)_settingsReader.GetTaskPercentageReportInterval(),
                MinTaskPercentageStep,
                MaxTaskPercentageStep);
            ShowScanMilestoneCount = _settingsReader.GetShowScanMilestoneCount();
            ScanMilestoneInterval = Math.Clamp(
                (int)_settingsReader.GetScanMilestoneInterval(),
                MinScanMilestoneInterval,
                MaxScanMilestoneInterval);
            SelectedScanMilestoneBasis = _settingsReader.GetScanMilestoneBasis();
            FolderBrowserStartAtLastLocation = _settingsReader.GetFolderBrowserStartAtLastLocation();
            ReloadPinnedFolders();
            ReloadMutedDroppedSongFolders();
        }
        finally
        {
            _isLoadingSettings = false;
        }

        return Task.CompletedTask;
    }

    partial void OnSelectedFolderChanged(string? value)
    {
        _isUpdatingFolderSelection = true;
        try
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedFolderIncludeSubdirectories = false;
                return;
            }

            SelectedFolderIncludeSubdirectories =
                _folderRecursionByPath.TryGetValue(value, out var includeSubdirectories)
                && includeSubdirectories;
        }
        finally
        {
            _isUpdatingFolderSelection = false;
        }
    }

    partial void OnSelectedFolderIncludeSubdirectoriesChanged(bool value)
    {
        if (_isLoadingSettings || _isUpdatingFolderSelection || string.IsNullOrWhiteSpace(SelectedFolder))
        {
            return;
        }

        _folderRecursionByPath[SelectedFolder] = value;
        _settingsWriter.SetFolderIncludeSubdirectories(SelectedFolder, value);
    }

    partial void OnAutoScanOnFolderAddChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetAutoScanOnFolderAdd(value);
    }

    partial void OnShowTaskPercentageChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetShowTaskPercentage(value);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnTaskPercentageReportIntervalChanged(int value)
    {
        var clamped = Math.Clamp(value, MinTaskPercentageStep, MaxTaskPercentageStep);
        if (clamped != value)
        {
            TaskPercentageReportInterval = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetTaskPercentageReportInterval((short)clamped);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnShowScanMilestoneCountChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetShowScanMilestoneCount(value);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnScanMilestoneIntervalChanged(int value)
    {
        var clamped = Math.Clamp(value, MinScanMilestoneInterval, MaxScanMilestoneInterval);
        if (clamped != value)
        {
            ScanMilestoneInterval = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetScanMilestoneInterval((short)clamped);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnSelectedScanMilestoneBasisChanged(TaskStatusCountBasis value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetScanMilestoneBasis(value);
        _backgroundTaskStatusService.RefreshSnapshot();
    }

    partial void OnFolderBrowserStartAtLastLocationChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetFolderBrowserStartAtLastLocation(value);
    }

    [RelayCommand]
    private Task OpenFolderBrowser() =>
        ExecuteSafeAsync(async ct =>
        {
            Logger.Information("[SettingsLibraryTabViewModel] Opening folder browser shell...");
            await _windowManager.ShowWindowAsync<FolderBrowserShellViewModel>(
                WindowShowOptions.CenteredOnMainWindow(),
                ct);
        });

    [RelayCommand]
    private Task RemoveFolder() =>
        ExecuteSafeAsync(async _ =>
        {
            var folderToRemove = SelectedFolder;
            if (string.IsNullOrWhiteSpace(folderToRemove))
            {
                return;
            }

            Logger.Information("[SettingsLibraryTabViewModel] Removing folder: {Folder}", folderToRemove);
            Folders.Remove(folderToRemove);
            _folderRecursionByPath.Remove(folderToRemove);
            await _fromFolderRemover.RemoveFromFolderAsync(folderToRemove);
            PersistMusicFolders();
            Logger.Verbose("[SettingsLibraryTabViewModel] Folder removed: {Folder}", folderToRemove);
        });

    [RelayCommand]
    private Task RemovePinnedFolder() =>
        ExecuteSafeAsync(_ =>
        {
            if (string.IsNullOrWhiteSpace(SelectedPinnedFolder))
            {
                return Task.CompletedTask;
            }

            PinnedFolders.Remove(SelectedPinnedFolder);
            PersistPinnedFolders();
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task ClearInvalidPins() =>
        ExecuteSafeAsync(_ =>
        {
            var cleanedPinnedFolders = _pinnedFoldersService.NormalizeExisting(PinnedFolders);

            PinnedFolders.Clear();
            foreach (var pinnedFolder in cleanedPinnedFolders)
            {
                PinnedFolders.Add(pinnedFolder);
            }

            PersistPinnedFolders();
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task RemoveMutedDroppedSongFolder() =>
        ExecuteSafeAsync(_ =>
        {
            if (string.IsNullOrWhiteSpace(SelectedMutedDroppedSongFolder))
            {
                return Task.CompletedTask;
            }

            MutedDroppedSongFolders.Remove(SelectedMutedDroppedSongFolder);
            PersistMutedDroppedSongFolders();
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task ResetDroppedFolderPrompts() =>
        ExecuteSafeAsync(_ =>
        {
            MutedDroppedSongFolders.Clear();
            PersistMutedDroppedSongFolders();
            return Task.CompletedTask;
        });

    [RelayCommand]
    private Task ForceScan() =>
        ExecuteSafeAsync(_ =>
        {
            Logger.Information("[SettingsLibraryTabViewModel] Force scanning folders...");
            return _folderScanner.ScanAllAsync(ScanMode.FullRefresh);
        });

    private void PersistMusicFolders()
    {
        var folders = Folders.Select(path => new FolderScanRequest(
            path,
            _folderRecursionByPath.TryGetValue(path, out var includeSubdirectories) && includeSubdirectories));
        _settingsWriter.SetMusicFolders(folders);
    }

    private void PersistPinnedFolders()
    {
        _settingsWriter.SetPinnedFolders(_pinnedFoldersService.Normalize(PinnedFolders));
    }

    private void ReloadPinnedFolders()
    {
        var pinnedFolders = _pinnedFoldersService.Normalize(_settingsReader.GetPinnedFolders());

        PinnedFolders.Clear();
        foreach (var pinnedFolder in pinnedFolders)
        {
            PinnedFolders.Add(pinnedFolder);
        }
    }

    private void PersistMutedDroppedSongFolders()
    {
        _settingsWriter.SetMutedDroppedSongFolders(MutedDroppedSongFolders);
    }

    private void ReloadMutedDroppedSongFolders()
    {
        var mutedFolders = _settingsReader.GetMutedDroppedSongFolders();

        MutedDroppedSongFolders.Clear();
        foreach (var mutedFolder in mutedFolders)
        {
            MutedDroppedSongFolders.Add(mutedFolder);
        }
    }
    
    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        FontFamilyName = message.Value;
        Logger.Debug("[SettingsLibraryTabViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
    }

    private async void OnFolderBrowserPathSelectedMessage(FolderBrowserPathSelectedMessage message)
    {
        try
        {
            var path = message.Value;
            if (string.IsNullOrWhiteSpace(path) || Folders.Contains(path))
            {
                return;
            }

            Logger.Information("[SettingsLibraryTabViewModel] Adding path to music folders: {Path}", path);
            Folders.Add(path);
            _folderRecursionByPath[path] = false;
            PersistMusicFolders();

            if (!AutoScanOnFolderAdd)
            {
                Logger.Information("[SettingsLibraryTabViewModel] Auto-scan on folder add is disabled.");
                return;
            }

            Logger.Information("[SettingsLibraryTabViewModel] Scanning newly added folder: {Path}", path);
            await _folderScanner.ScanAsync(path, ScanMode.Incremental);
        }
        catch (Exception e)
        {
            Logger.Error(e, "[SettingsLibraryTabViewModel] An error occurred while adding a folder:" +
                            "{Path}", message.Value);
        }
    }

    private void OnPinnedFoldersChangedMessage(PinnedFoldersChangedMessage message)
    {
        var normalizedPins = _pinnedFoldersService.Normalize(message.Value);
        PinnedFolders = new ObservableCollection<string>(normalizedPins);
    }
}
