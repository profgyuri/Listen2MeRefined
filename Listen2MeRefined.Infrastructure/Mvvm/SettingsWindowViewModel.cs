using System.Windows.Media;

namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System.Collections.ObjectModel;

[INotifyPropertyChanged]
public partial class SettingsWindowViewModel : 
    INotificationHandler<FolderBrowserNotification>
{
    private readonly ILogger _logger;
    private readonly ISettingsManager _settingsManager;
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IFileEnumerator _fileEnumerator;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly IMediator _mediator;

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private string? _selectedFolder;
    [ObservableProperty] private FontFamily _selectedFontFamily;
    [ObservableProperty] private ObservableCollection<string> _folders;
    [ObservableProperty] private ObservableCollection<FontFamily> _fontFamilies;
    [ObservableProperty] private bool _isClearMetadataButtonVisible = true;
    [ObservableProperty] private bool _isCancelClearMetadataButtonVisible;

    public SettingsWindowViewModel(ILogger logger, ISettingsManager settingsManager, IFileAnalyzer<AudioModel> audioFileAnalyzer,
        IFileEnumerator fileEnumerator, IRepository<AudioModel> audioRepository, IMediator mediator)
    {
        _logger = logger;
        _settingsManager = settingsManager;
        _audioFileAnalyzer = audioFileAnalyzer;
        _fileEnumerator = fileEnumerator;
        _audioRepository = audioRepository;
        _mediator = mediator;
        _fontFamilies = new ObservableCollection<FontFamily>(Fonts.SystemFontFamilies);
        _selectedFontFamily = new FontFamily(_settingsManager.Settings.FontFamily);

        Init();
    }

    private void Init()
    {
        var settings = _settingsManager.Load();
        Folders = new(settings.MusicFolders);
        FontFamily = settings.FontFamily;
        SelectedFontFamily = new FontFamily(settings.FontFamily);
        FontFamilies = new ObservableCollection<FontFamily>(Fonts.SystemFontFamilies);
    }

    #region Commands
    [RelayCommand]
    private void RemoveFolder()
    {
        _logger.Debug("Removing folder: {0}", SelectedFolder);

        Folders.Remove(SelectedFolder!);

        _settingsManager.Save(s => s.MusicFolders = _folders);
    }
    
    [RelayCommand]
    private async Task ClearMetadata()
    {
        _logger.Debug("Clearing metadata...");
        
        //await _audioRepository.DeleteAllAsync();
        IsClearMetadataButtonVisible = false;
        IsCancelClearMetadataButtonVisible = true;
        
        _logger.Debug("Metadata cleared");
    }
    #endregion

    #region Notification Handlers
    public async Task Handle(FolderBrowserNotification notification, CancellationToken cancellationToken)
    {
        _logger.Debug("Adding path to music folders: {Path}", notification.Path);
        
        Folders.Add(notification.Path);
        
        _settingsManager.Save(s => s.MusicFolders = _folders);
        
        _logger.Information("Scanning folder for audio files: {Path}", notification.Path);
        var files = await _fileEnumerator.EnumerateFilesAsync(notification.Path);
        var songs = await _audioFileAnalyzer.AnalyzeAsync(files);
        await _audioRepository.CreateAsync(songs);
    }
    #endregion

    partial void OnSelectedFontFamilyChanged(FontFamily value)
    {
        OnPropertyChanged(nameof(SelectedFontFamily));
        _settingsManager.Save(s => s.FontFamily = value.Source);
        _mediator.Publish(new FontFamilyChangedNotification(value));
    }
}