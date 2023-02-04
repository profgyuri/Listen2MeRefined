using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Media;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using Source;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Mvvm;

public sealed partial class SettingsWindowViewModel : 
    ObservableObject,
    INotificationHandler<FolderBrowserNotification>
{
    private readonly ILogger _logger;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IRepository<MusicFolderModel> _musicFolderRepository;
    private readonly IRepository<PlaylistModel> _playlistRepository;
    private readonly IFolderScanner _folderScanner;
    private readonly IMediator _mediator;
    private readonly FontFamilies _installedFontFamilies;

    private TimedTask? _timedTask;
    private int _secondsToCancelClear = 5;

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private string? _selectedFolder;
    [ObservableProperty] private string _selectedFontFamily;
    [ObservableProperty] private AudioOutputDevice _selectedAudioOutputDevice;
    [ObservableProperty] private ObservableCollection<string> _folders;
    [ObservableProperty] private ObservableCollection<string> _fontFamilies;
    [ObservableProperty] private ObservableCollection<AudioOutputDevice> _audioOutputDevices = new();
    [ObservableProperty] private bool _isClearMetadataButtonVisible = true;
    [ObservableProperty] private bool _isCancelClearMetadataButtonVisible;
    [ObservableProperty] private string _cancelClearMetadataButtonContent = "Cancel(5)";
    
    public bool ScanOnStartup
    {
        get => _settingsManager.Settings.ScanOnStartup;
        set
        {
            _settingsManager.SaveSettings(s => s.ScanOnStartup = value);
            OnPropertyChanged();
        }
    }

    public bool DontScanOnStartup => !ScanOnStartup;

    public SettingsWindowViewModel(
        ILogger logger,
        ISettingsManager<AppSettings> settingsManager,
        IRepository<AudioModel> audioRepository,
        IMediator mediator,
        FontFamilies installedFontFamilies,
        IRepository<MusicFolderModel> musicFolderRepository,
        IRepository<PlaylistModel> playlistRepository,
        IFolderScanner folderScanner)
    {
        _logger = logger;
        _settingsManager = settingsManager;
        _audioRepository = audioRepository;
        _mediator = mediator;
        _installedFontFamilies = installedFontFamilies;
        _musicFolderRepository = musicFolderRepository;
        _playlistRepository = playlistRepository;
        _folderScanner = folderScanner;

        Initialize().ConfigureAwait(false);
    }

    private async Task Initialize()
    {
        await Task.Run(async () =>
        {
            FontFamilies = new(_installedFontFamilies.FontFamilyNames);

            var settings = _settingsManager.Settings;
            Folders = new(settings.MusicFolders.Select(x => x.FullPath));
            FontFamily = settings.FontFamily;
            SelectedFontFamily = string.IsNullOrEmpty(settings.FontFamily) ? "Segoe UI" : settings.FontFamily;
            ScanOnStartup = settings.ScanOnStartup;
            await GetAudioOutputDevices();
        });
    }

    partial void OnSelectedFontFamilyChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedFontFamily));
        _settingsManager.SaveSettings(s => s.FontFamily = value);
        _mediator.Publish(new FontFamilyChangedNotification(value));
    }

    partial void OnSelectedAudioOutputDeviceChanged(AudioOutputDevice value)
    {
        OnPropertyChanged(nameof(SelectedAudioOutputDevice));
        _mediator.Publish(new AudioOutputDeviceChangedNotification(value));
    }

    #region Notification Handlers
    public async Task Handle(
        FolderBrowserNotification notification,
        CancellationToken cancellationToken)
    {
        var path = notification.Path;

        if (Folders.Contains(path))
        {
            return;
        }

        _logger.Debug("Adding path to music folders: {Path}", path);

        Folders.Add(path);

        _settingsManager.SaveSettings(s => s.MusicFolders = Folders.Select(x => new MusicFolderModel(x)).ToList());

        await _folderScanner.ScanAsync(path);
    }
    #endregion

    #region Commands
    [RelayCommand]
    private void RemoveFolder()
    {
        _logger.Debug("Removing folder: {Folder}", SelectedFolder);

        Folders.Remove(SelectedFolder!);

        _settingsManager.SaveSettings(s => s.MusicFolders = Folders.Select(x => new MusicFolderModel(x)).ToList());
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
                await _audioRepository.RemoveAllAsync();
                await _musicFolderRepository.RemoveAllAsync();
                await _playlistRepository.RemoveAllAsync();
                
                Folders = new();

                await _timedTask?.StopAsync()!;
                IsClearMetadataButtonVisible = true;
                IsCancelClearMetadataButtonVisible = false;
                _secondsToCancelClear = 5;
                CancelClearMetadataButtonContent = $"Cancel ({_secondsToCancelClear})";

                _logger.Debug("Metadata cleared");
            }

            _secondsToCancelClear--;
            CancelClearMetadataButtonContent = $"Cancel ({_secondsToCancelClear})";
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
    
    [RelayCommand]
    private async Task ForceScanAsync()
    {
        _logger.Debug("Force scanning folders...");

        await _folderScanner.ScanAllAsync();
    }
    #endregion

    private async Task GetAudioOutputDevices()
    {
        var devices = await AudioDevices.GetOutputDevices();
        foreach (var device in devices)
        {
            AudioOutputDevices.Add(device);
            OnPropertyChanged(nameof(AudioOutputDevices));
        }
        
        SelectedAudioOutputDevice = AudioOutputDevices[0];
    }
}