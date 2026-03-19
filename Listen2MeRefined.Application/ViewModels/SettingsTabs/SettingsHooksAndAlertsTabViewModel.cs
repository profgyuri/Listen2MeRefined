using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.SettingsTabs;

public partial class SettingsHooksAndAlertsTabViewModel : ViewModelBase
{
    private const int MinCornerTriggerSizePx = 4;
    private const int MaxCornerTriggerSizePx = 64;
    private const int MinCornerDebounceMs = 5;
    private const int MaxCornerDebounceMs = 200;

    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly IGlobalHookSettingsSyncService _globalHookSettingsSyncService;
    private bool _isLoadingSettings;
    private bool _isSyncingGlobalHookState;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _newSongWindowPositions = [];
    [ObservableProperty] private string _selectedNewSongWindowPosition = string.Empty;
    [ObservableProperty] private bool _enableGlobalMediaKeys;
    [ObservableProperty] private bool _enableCornerNowPlayingPopup;
    [ObservableProperty] private int _cornerTriggerSizePx = 10;
    [ObservableProperty] private int _cornerTriggerDebounceMs = 10;

    public SettingsHooksAndAlertsTabViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        IGlobalHookSettingsSyncService globalHookSettingsSyncService) : base(errorHandler, logger, messenger)
    {
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _globalHookSettingsSyncService = globalHookSettingsSyncService;
    }

    public override Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);

        _isLoadingSettings = true;
        try
        {
            NewSongWindowPositions = ["Default", "Always on top"];
            var selectedWindowPosition = _settingsReader.GetNewSongWindowPosition();
            SelectedNewSongWindowPosition = string.IsNullOrWhiteSpace(selectedWindowPosition)
                ? "Default"
                : selectedWindowPosition;

            EnableGlobalMediaKeys = _settingsReader.GetEnableGlobalMediaKeys();
            EnableCornerNowPlayingPopup = _settingsReader.GetEnableCornerNowPlayingPopup();
            CornerTriggerSizePx = Math.Clamp(
                (int)_settingsReader.GetCornerTriggerSizePx(),
                MinCornerTriggerSizePx,
                MaxCornerTriggerSizePx);
            CornerTriggerDebounceMs = Math.Clamp(
                (int)_settingsReader.GetCornerTriggerDebounceMs(),
                MinCornerDebounceMs,
                MaxCornerDebounceMs);

            var selectedFont = _settingsReader.GetFontFamily();
            FontFamilyName = string.IsNullOrWhiteSpace(selectedFont)
                ? "Segoe UI"
                : selectedFont;
        }
        finally
        {
            _isLoadingSettings = false;
        }

        return Task.CompletedTask;
    }

    partial void OnSelectedNewSongWindowPositionChanged(string value)
    {
        if (_isLoadingSettings || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        Logger.Information("[SettingsHooksAndAlertsTabViewModel] New song window position changed to: {Position}", value);
        _settingsWriter.SetNewSongWindowPosition(value);
        Messenger.Send(new CornerWindowPositionChangedMessage(value));
    }

    partial void OnEnableGlobalMediaKeysChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetEnableGlobalMediaKeys(value);
        _ = ExecuteSafeAsync(_ => SyncGlobalHookRegistrationAsync());
    }

    partial void OnEnableCornerNowPlayingPopupChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetEnableCornerNowPlayingPopup(value);
        _ = ExecuteSafeAsync(_ => SyncGlobalHookRegistrationAsync());
    }

    partial void OnCornerTriggerSizePxChanged(int value)
    {
        var clamped = Math.Clamp(value, MinCornerTriggerSizePx, MaxCornerTriggerSizePx);
        if (clamped != value)
        {
            CornerTriggerSizePx = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetCornerTriggerSizePx((short)clamped);
    }

    partial void OnCornerTriggerDebounceMsChanged(int value)
    {
        var clamped = Math.Clamp(value, MinCornerDebounceMs, MaxCornerDebounceMs);
        if (clamped != value)
        {
            CornerTriggerDebounceMs = clamped;
            return;
        }

        if (_isLoadingSettings)
        {
            return;
        }

        _settingsWriter.SetCornerTriggerDebounceMs((short)clamped);
    }

    private async Task SyncGlobalHookRegistrationAsync()
    {
        if (_isSyncingGlobalHookState)
        {
            return;
        }

        _isSyncingGlobalHookState = true;
        try
        {
            await _globalHookSettingsSyncService
                .SyncAsync(EnableGlobalMediaKeys, EnableCornerNowPlayingPopup);
        }
        finally
        {
            _isSyncingGlobalHookState = false;
        }
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        FontFamilyName = message.Value;
        Logger.Debug("[SettingsHooksAndAlertsTabViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
    }
}
