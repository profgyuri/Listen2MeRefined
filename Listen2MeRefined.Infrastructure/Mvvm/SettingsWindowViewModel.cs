using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using Source;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class SettingsWindowViewModel :
    INotificationHandler<FolderBrowserNotification>
{
    private readonly ILogger _logger;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IFileEnumerator _fileEnumerator;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IMediator _mediator;

    private TimedTask? _timedTask;
    private int _secondsToCancelClear = 5;

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private string? _selectedFolder;
    [ObservableProperty] private string _selectedFontFamily;
    [ObservableProperty] private ObservableCollection<string> _folders;
    [ObservableProperty] private ObservableCollection<string> _fontFamilies;
    [ObservableProperty] private bool _isClearMetadataButtonVisible = true;
    [ObservableProperty] private bool _isCancelClearMetadataButtonVisible;
    [ObservableProperty] private string _cancelClearMetadataButtonContent = "Cancel(5)";

    public SettingsWindowViewModel(ILogger logger, ISettingsManager<AppSettings> settingsManager,
        IFileAnalyzer<AudioModel> audioFileAnalyzer,
        IFileEnumerator fileEnumerator, IRepository<AudioModel> audioRepository, IMediator mediator,
        FontFamilies fontFamilies)
    {
        _logger = logger;
        _settingsManager = settingsManager;
        _audioFileAnalyzer = audioFileAnalyzer;
        _fileEnumerator = fileEnumerator;
        _audioRepository = audioRepository;
        _mediator = mediator;

        FontFamilies = new ObservableCollection<string>(fontFamilies.FontFamilyNames);

        Init();
    }

    private void Init()
    {
        var settings = _settingsManager.Settings;
        Folders = new(settings.MusicFolders);
        FontFamily = settings.FontFamily;
        SelectedFontFamily = settings.FontFamily;
    }

    partial void OnSelectedFontFamilyChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedFontFamily));
        _settingsManager.SaveSettings(s => s.FontFamily = value);
        _mediator.Publish(new FontFamilyChangedNotification(value));
    }

    #region Notification Handlers
    public async Task Handle(FolderBrowserNotification notification, CancellationToken cancellationToken)
    {
        _logger.Debug("Adding path to music folders: {Path}", notification.Path);

        Folders.Add(notification.Path);

        _settingsManager.SaveSettings(s => s.MusicFolders = _folders);

        _logger.Information("Scanning folder for audio files: {Path}", notification.Path);
        var files = await _fileEnumerator.EnumerateFilesAsync(notification.Path);
        var songs = await _audioFileAnalyzer.AnalyzeAsync(files);
        await _audioRepository.CreateAsync(songs);
    }
    #endregion

    #region Commands
    [RelayCommand]
    private void RemoveFolder()
    {
        _logger.Debug("Removing folder: {Folder}", SelectedFolder);

        Folders.Remove(SelectedFolder!);

        _settingsManager.SaveSettings(s => s.MusicFolders = _folders);
    }

    [RelayCommand]
    private void ClearMetadata()
    {
        _logger.Debug("Clearing metadata...");

        _timedTask = new();
        _timedTask.Start(TimeSpan.FromSeconds(1), async () =>
        {
            if (_secondsToCancelClear == 0)
            {
                await _audioRepository.DeleteAllAsync();

                await _timedTask?.StopAsync()!;
                IsClearMetadataButtonVisible = true;
                IsCancelClearMetadataButtonVisible = false;
                _secondsToCancelClear = 5;
                CancelClearMetadataButtonContent = $"Cancel({_secondsToCancelClear})";

                _logger.Debug("Metadata cleared");
            }

            _secondsToCancelClear--;
            CancelClearMetadataButtonContent = $"Cancel({_secondsToCancelClear})";
        });
        IsClearMetadataButtonVisible = false;
        IsCancelClearMetadataButtonVisible = true;
    }

    [RelayCommand]
    private async Task CancelClearMetadataAsync()
    {
        _logger.Debug("Clearing metadata canceled");

        await _timedTask?.StopAsync()!;
        IsClearMetadataButtonVisible = true;
        IsCancelClearMetadataButtonVisible = false;
        _secondsToCancelClear = 5;
        CancelClearMetadataButtonContent = $"Cancel({_secondsToCancelClear})";
    }
    #endregion
}