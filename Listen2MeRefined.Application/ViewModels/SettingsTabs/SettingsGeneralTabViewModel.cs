using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Application.Utils;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.SettingsTabs;

public partial class SettingsGeneralTabViewModel : ViewModelBase
{
    private const string CompactPlaylistViewMode = "Compact";
    private const string DetailedPlaylistViewMode = "Detailed";
    private const int MinSearchDebounceMs = 100;
    private const int MaxSearchDebounceMs = 2000;

    private readonly FontFamilies _installedFontFamilies;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IAppUpdateChecker _appUpdateChecker;
    private readonly IVersionChecker _versionChecker;
    private readonly IAppThemeService _appThemeService;
    private bool _isLoadingSettings;

    [ObservableProperty] private ObservableCollection<string> _fontFamilies = [];
    [ObservableProperty] private string _selectedFontFamily = string.Empty;
    [ObservableProperty] private string _selectedPlaylistViewMode = DetailedPlaylistViewMode;
    [ObservableProperty] private ObservableCollection<string> _themeModes = [];
    [ObservableProperty] private ObservableCollection<string> _accentColors = [];
    [ObservableProperty] private string _selectedThemeMode = "Dark";
    [ObservableProperty] private string _selectedAccentColor = "Orange";
    [ObservableProperty] private bool _autoCheckUpdatesOnStartup = true;
    [ObservableProperty] private string _updateAvailableText = string.Empty;
    [ObservableProperty] private bool _isUpdateButtonVisible;
    [ObservableProperty] private bool _autoFlowTrackText;
    [ObservableProperty] private int _searchDebounceMs = 300;

    public ObservableCollection<string> PlaylistViewModes { get; } =
        new([DetailedPlaylistViewMode, CompactPlaylistViewMode]);

    public SettingsGeneralTabViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        FontFamilies installedFontFamilies,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IAppUpdateChecker appUpdateChecker,
        IVersionChecker versionChecker,
        IAppThemeService appThemeService) : base(errorHandler, logger, messenger)
    {
        _installedFontFamilies = installedFontFamilies;
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _appUpdateChecker = appUpdateChecker;
        _versionChecker = versionChecker;
        _appThemeService = appThemeService;
    }

    public override async Task InitializeAsync(CancellationToken ct = default)
    {
        _isLoadingSettings = true;
        try
        {
            FontFamilies = new ObservableCollection<string>(_installedFontFamilies.FontFamilyNames);
            ThemeModes = new ObservableCollection<string>(_appThemeService.GetThemeModes());
            AccentColors = new ObservableCollection<string>(_appThemeService.GetAccentColors());

            var selectedFont = _settingsReader.GetFontFamily();
            SelectedFontFamily = string.IsNullOrWhiteSpace(selectedFont)
                ? "Segoe UI"
                : selectedFont;
            SelectedPlaylistViewMode = _settingsReader.GetUseCompactPlaylistView()
                ? CompactPlaylistViewMode
                : DetailedPlaylistViewMode;
            AutoCheckUpdatesOnStartup = _settingsReader.GetAutoCheckUpdatesOnStartup();
            SelectedThemeMode = _settingsReader.GetThemeMode();
            SelectedAccentColor = _settingsReader.GetAccentColor();
            AutoFlowTrackText = _settingsReader.GetAutoFlowTrackText();
            SearchDebounceMs = _settingsReader.GetSearchDebounceMs();
        }
        finally
        {
            _isLoadingSettings = false;
        }

        if (AutoCheckUpdatesOnStartup)
        {
            await CheckForUpdatesAsync();
            return;
        }

        UpdateAvailableText = "Automatic update checks are disabled.";
        IsUpdateButtonVisible = false;
    }

    partial void OnSelectedFontFamilyChanged(string value)
    {
        if (_isLoadingSettings || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        Logger.Information("[SettingsGeneralTabViewModel] Font family changed to: {FontFamily}", value);
        _settingsWriter.SetFontFamily(value);
        Messenger.Send(new FontFamilyChangedMessage(value));
    }

    partial void OnSelectedPlaylistViewModeChanged(string value)
    {
        if (_isLoadingSettings || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var useCompactPlaylistView = string.Equals(value, CompactPlaylistViewMode, StringComparison.Ordinal);
        _settingsWriter.SetUseCompactPlaylistView(useCompactPlaylistView);
        Messenger.Send(new PlaylistViewModeChangedMessage(useCompactPlaylistView));
    }

    partial void OnSelectedThemeModeChanged(string value)
    {
        if (_isLoadingSettings || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _settingsWriter.SetThemeMode(value);
        _appThemeService.ApplyTheme(value, SelectedAccentColor);
    }

    partial void OnSelectedAccentColorChanged(string value)
    {
        if (_isLoadingSettings || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _settingsWriter.SetAccentColor(value);
        _appThemeService.ApplyTheme(SelectedThemeMode, value);
    }

    partial void OnAutoCheckUpdatesOnStartupChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetAutoCheckUpdatesOnStartup(value);
        if (!value)
        {
            UpdateAvailableText = "Automatic update checks are disabled.";
            IsUpdateButtonVisible = false;
        }
    }

    partial void OnAutoFlowTrackTextChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetAutoFlowTrackText(value);
        Messenger.Send(new AutoFlowTrackTextChangedMessage(value));
    }

    partial void OnSearchDebounceMsChanged(int value)
    {
        var clamped = Math.Clamp(value, MinSearchDebounceMs, MaxSearchDebounceMs);
        if (clamped != value)
        {
            SearchDebounceMs = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetSearchDebounceMs((short)clamped);
        Messenger.Send(new SearchDebounceChangedMessage((short)clamped));
    }

    [RelayCommand]
    private Task CheckForUpdatesNowAsync() =>
        ExecuteSafeAsync(_ => CheckForUpdatesAsync());

    [RelayCommand]
    private Task OpenBrowserForUpdate() =>
        ExecuteSafeAsync(async _ =>
        {
            Logger.Information("[SettingsGeneralTabViewModel] Opening browser to get update...");
            await Task.Run(_versionChecker.OpenUpdateLink);
        });

    private async Task CheckForUpdatesAsync()
    {
        var status = await _appUpdateChecker.CheckForUpdatesAsync();
        UpdateAvailableText = status.Message;
        IsUpdateButtonVisible = status.CanOpenUpdateLink;
    }
}
