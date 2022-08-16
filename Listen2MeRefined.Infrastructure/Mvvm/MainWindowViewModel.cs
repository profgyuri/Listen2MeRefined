﻿using System.Collections.ObjectModel;
using Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class MainWindowViewModel : 
    INotificationHandler<CurrentSongNotification>
{
    private readonly ILogger _logger;
    private readonly IMediaController _mediaController;
    private readonly IRepository<AudioModel> _audioRepository;
    
    private readonly TimedTask _timedTask;

    [ObservableProperty] private string _fontFamily = "Comic Sans MS";
    [ObservableProperty] private string _searchTerm = "";
    [ObservableProperty] private AudioModel? _selectedSong;
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private ObservableCollection<AudioModel> _playList = new();
    
    public double CurrentTime
    {
        get => _mediaController?.CurrentTime ?? 0;
        set  {
            _mediaController.CurrentTime = value;
            OnPropertyChanged();
        }
    }

    public MainWindowViewModel(IMediaController mediaController, ILogger logger, IPlaylistReference playlistReference,
        IRepository<AudioModel> audioRepository, TimedTask timedTask)
    {
        _mediaController = mediaController;
        _logger = logger;
        _audioRepository = audioRepository;
        _timedTask = timedTask;

        playlistReference.PassPlaylist(ref _playList);
        _timedTask.Start(() => OnPropertyChanged(nameof(CurrentTime)));
    }

    #region Implementation of INotificationHandler<in CurrentSongNotification>
    /// <inheritdoc />
    public Task Handle(CurrentSongNotification notification, CancellationToken cancellationToken)
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