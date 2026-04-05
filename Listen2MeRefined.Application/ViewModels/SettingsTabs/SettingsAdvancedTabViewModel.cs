using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Folders;
using Listen2MeRefined.Application.Messages;
using Listen2MeRefined.Application.Settings;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.SettingsTabs;

public partial class SettingsAdvancedTabViewModel : ViewModelBase
{
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IRepository<MusicFolderModel> _musicFolderRepository;
    private readonly IRepository<PlaylistModel> _playlistRepository;
    private readonly IAppSettingsReader _settingsReader;
    private readonly IAppSettingsWriter _settingsWriter;
    private readonly int _countdownStartSeconds;
    private readonly TimeSpan _countdownTickInterval;

    private int _secondsToCancelClear;
    private CancellationTokenSource? _clearMetadataCts;
    private Task? _clearMetadataTask;

    [ObservableProperty] private string _fontFamilyName = string.Empty;
    [ObservableProperty] private bool _isClearMetadataButtonVisible = true;
    [ObservableProperty] private bool _isCancelClearMetadataButtonVisible;
    [ObservableProperty] private string _cancelClearMetadataButtonContent = string.Empty;

    public SettingsAdvancedTabViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IRepository<AudioModel> audioRepository,
        IRepository<MusicFolderModel> musicFolderRepository,
        IRepository<PlaylistModel> playlistRepository,
        IAppSettingsReader settingsReader,
        IAppSettingsWriter settingsWriter,
        int countdownStartSeconds = 5,
        TimeSpan? countdownTickInterval = null) : base(errorHandler, logger, messenger)
    {
        if (countdownStartSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(countdownStartSeconds),
                countdownStartSeconds,
                "Countdown start seconds must be non-negative.");
        }

        _audioRepository = audioRepository;
        _musicFolderRepository = musicFolderRepository;
        _playlistRepository = playlistRepository;
        _settingsReader = settingsReader;
        _settingsWriter = settingsWriter;
        _countdownStartSeconds = countdownStartSeconds;
        _countdownTickInterval = countdownTickInterval ?? TimeSpan.FromSeconds(1);

        if (_countdownTickInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(countdownTickInterval),
                _countdownTickInterval,
                "Countdown tick interval must be greater than zero.");
        }

        ResetClearMetadataUi();
    }

    public override Task InitializeAsync(CancellationToken ct = default)
    {
        RegisterMessage<FontFamilyChangedMessage>(OnFontFamilyChangedMessage);

        FontFamilyName = _settingsReader.GetFontFamily();

        ResetClearMetadataUi();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ClearMetadata() =>
        ExecuteSafeAsync(async _ =>
        {
            if (_clearMetadataTask is { IsCompleted: false })
            {
                return;
            }

            Logger.Information("[SettingsAdvancedTabViewModel] Clearing metadata...");
            _secondsToCancelClear = _countdownStartSeconds;
            CancelClearMetadataButtonContent = BuildCancelButtonContent(_secondsToCancelClear);
            IsClearMetadataButtonVisible = false;
            IsCancelClearMetadataButtonVisible = true;

            _clearMetadataCts?.Dispose();
            _clearMetadataCts = new CancellationTokenSource();
            _clearMetadataTask = RunClearMetadataCountdownAsync(_clearMetadataCts.Token);

            await _clearMetadataTask;
        });

    [RelayCommand]
    private Task CancelClearMetadata() =>
        ExecuteSafeAsync(async _ =>
        {
            if (_clearMetadataTask is null)
            {
                ResetClearMetadataUi();
                return;
            }

            Logger.Information("[SettingsAdvancedTabViewModel] Clearing metadata canceled.");
            _clearMetadataCts?.Cancel();
            await _clearMetadataTask;
        });

    private async Task RunClearMetadataCountdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_secondsToCancelClear > 0)
            {
                await Task.Delay(_countdownTickInterval, cancellationToken);
                _secondsToCancelClear--;
                CancelClearMetadataButtonContent = BuildCancelButtonContent(_secondsToCancelClear);
            }

            await ClearMetadataCoreAsync();
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected when user presses "Cancel".
        }
        finally
        {
            _clearMetadataCts?.Dispose();
            _clearMetadataCts = null;
            _clearMetadataTask = null;
            ResetClearMetadataUi();
        }
    }

    private async Task ClearMetadataCoreAsync()
    {
        Logger.Verbose("[SettingsAdvancedTabViewModel] Removing entries from database...");
        await _audioRepository.RemoveAllAsync();
        await _musicFolderRepository.RemoveAllAsync();
        await _playlistRepository.RemoveAllAsync();

        _settingsWriter.SetMusicFolders(Array.Empty<FolderScanRequest>());
        Logger.Debug("[SettingsAdvancedTabViewModel] Metadata cleared.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _clearMetadataCts?.Cancel();
            _clearMetadataCts?.Dispose();
            _clearMetadataCts = null;
        }

        base.Dispose(disposing);
    }

    private void OnFontFamilyChangedMessage(FontFamilyChangedMessage message)
    {
        FontFamilyName = message.Value;
        Logger.Debug("[SettingsAdvancedTabViewModel] Received FontFamilyChangedMessage: {message}", message.Value);
    }

    private void ResetClearMetadataUi()
    {
        _secondsToCancelClear = _countdownStartSeconds;
        CancelClearMetadataButtonContent = BuildCancelButtonContent(_secondsToCancelClear);
        IsClearMetadataButtonVisible = true;
        IsCancelClearMetadataButtonVisible = false;
    }

    private static string BuildCancelButtonContent(int secondsToCancel)
    {
        return $"Cancel ({secondsToCancel})";
    }
}
