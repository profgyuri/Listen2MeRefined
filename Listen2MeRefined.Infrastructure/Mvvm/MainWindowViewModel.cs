namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class MainWindowViewModel
{
    #region Fields
    private IMediaController _mediaController;
    #endregion

    #region Properties
    [ObservableProperty] private string _fontFamily;
    #endregion

    public MainWindowViewModel(IMediaController mediaController)
    {
        _mediaController = mediaController;
        _fontFamily = "Comic Sans MS";
    }

    #region Commands
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