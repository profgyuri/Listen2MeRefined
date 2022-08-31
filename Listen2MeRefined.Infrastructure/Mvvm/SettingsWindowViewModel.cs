using System.Windows.Media;

namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using System.Collections.ObjectModel;

[INotifyPropertyChanged]
public partial class SettingsWindowViewModel : INotificationHandler<FolderBrowserNotification>
{
    private readonly ILogger _logger;
    private readonly ISettingsManager _settingsManager;
    private readonly IFileAnalyzer<AudioModel> _audioFileAnalyzer;
    private readonly IFileEnumerator _fileEnumerator;
    private readonly IRepository<AudioModel> _audioRepository;

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private string? _selectedFolder;
    [ObservableProperty] private FontFamily _selectedFontFamily;
    [ObservableProperty] private ObservableCollection<string> _folders;
    [ObservableProperty] private ObservableCollection<FontFamily> _fontFamilies;

    public SettingsWindowViewModel(ILogger logger, ISettingsManager settingsManager, IFileAnalyzer<AudioModel> audioFileAnalyzer,
        IFileEnumerator fileEnumerator, IRepository<AudioModel> audioRepository)
    {
        _logger = logger;
        _settingsManager = settingsManager;
        _audioFileAnalyzer = audioFileAnalyzer;
        _fileEnumerator = fileEnumerator;
        _audioRepository = audioRepository;
        _fontFamilies = new ObservableCollection<FontFamily>(Fonts.SystemFontFamilies);

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

    [RelayCommand]
    private void RemoveFolder()
    {
        _logger.Debug("Removing folder: {0}", SelectedFolder);

        Folders.Remove(SelectedFolder!);

        _settingsManager.Save(s => s.MusicFolders = _folders);
    }

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
}