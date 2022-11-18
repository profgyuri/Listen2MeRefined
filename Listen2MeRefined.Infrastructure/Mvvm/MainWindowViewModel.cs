﻿using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;
using Source;
using Source.Storage;

namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class MainWindowViewModel
    : INotificationHandler<CurrentSongNotification>,
        INotificationHandler<FontFamilyChangedNotification>
{
    private readonly ILogger _logger;
    private readonly IMediaController _mediaController;
    private readonly IRepository<AudioModel> _audioRepository;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IGlobalHook _globalHook;

    [ObservableProperty] private string _fontFamily;
    [ObservableProperty] private string _searchTerm = "";
    [ObservableProperty] private AudioModel? _selectedSong;
    [ObservableProperty] private int _selectedIndex = -1;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private ObservableCollection<AudioModel> _playList = new();

    public double CurrentTime
    {
        get => _mediaController.CurrentTime;
        set
        {
            _mediaController.CurrentTime = value;
            OnPropertyChanged();
        }
    }

    public MainWindowViewModel(
        IMediaController mediaController,
        ILogger logger,
        IPlaylistReference playlistReference,
        IRepository<AudioModel> audioRepository,
        TimedTask timedTask,
        ISettingsManager<AppSettings> settingsManager,
        IGlobalHook globalHook)
    {
        _mediaController = mediaController;
        _logger = logger;
        _audioRepository = audioRepository;
        _settingsManager = settingsManager;
        _globalHook = globalHook;
        _fontFamily = _settingsManager.Settings.FontFamily;

        playlistReference.PassPlaylist(ref _playList);
        timedTask.Start(
            TimeSpan.FromMilliseconds(500),
            () => OnPropertyChanged(nameof(CurrentTime)));
        _globalHook.Register();
    }

    ~MainWindowViewModel()
    {
        _globalHook.Unregister();
    }

    #region Implementation of INotificationHandler<in FontFamilyChangedNotification>
    /// <inheritdoc />
    Task INotificationHandler<FontFamilyChangedNotification>.Handle(
        FontFamilyChangedNotification notification,
        CancellationToken cancellationToken)
    {
        FontFamily = notification.FontFamily;
        return Task.CompletedTask;
    }
    #endregion

    #region Implementation of INotificationHandler<in CurrentSongNotification>
    /// <inheritdoc />
    Task INotificationHandler<CurrentSongNotification>.Handle(
        CurrentSongNotification notification,
        CancellationToken cancellationToken)
    {
        SelectedSong = notification.Audio;
        return Task.CompletedTask;
    }
    #endregion

    #region Commands
    [RelayCommand]
    public async Task QuickSearch()
    {
        _logger.Information("Searching for \'{SearchTerm}\'", _searchTerm);
        _searchResults.Clear();
        var results =
            string.IsNullOrEmpty(_searchTerm)
                ? await _audioRepository.ReadAsync()
                : await _audioRepository.ReadAsync(_searchTerm);
        _searchResults.AddRange(results);
    }

    [RelayCommand]
    public void JumpToSelecteSong()
    {
        if (_selectedIndex > -1)
        {
            _mediaController.JumpToIndex(_selectedIndex);
        }
    }

    [RelayCommand]
    public void PlayPause()
    {
        _mediaController.PlayPause();
    }

    [RelayCommand]
    public void Stop()
    {
        _mediaController.Stop();
    }

    [RelayCommand]
    public void Next()
    {
        _mediaController.Next();
    }

    [RelayCommand]
    public void Previous()
    {
        _mediaController.Previous();
    }

    [RelayCommand]
    public void Shuffle()
    {
        _mediaController.Shuffle();
    }
    #endregion
}