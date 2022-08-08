namespace Listen2MeRefined.Infrastructure.Mvvm;

using System.Collections.ObjectModel;

[INotifyPropertyChanged]
public partial class MainWindowViewModel
{
    private readonly ILogger _logger;
    private readonly IMediaController _mediaController;

    [ObservableProperty] private string _fontFamily = "Comic Sans MS";
    [ObservableProperty] private string _searchTerm = "";
    [ObservableProperty] private ObservableCollection<AudioModel> _searchResults = new();
    [ObservableProperty] private ObservableCollection<AudioModel> _playList = new();

    public MainWindowViewModel(IMediaController mediaController, ILogger logger)
    {
        _mediaController = mediaController;
        _logger = logger;
    }

    #region Commands
    [RelayCommand]
    public void QuickSearch()
    {
        _logger.Information($"Searching for '{_searchTerm}'");
        _searchResults.Clear();
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