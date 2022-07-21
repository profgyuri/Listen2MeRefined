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
    }

    #region Commands
    [ICommand]
    public void PlayPause()
    {
        _mediaController.PlayPause();
    }

    [ICommand]
    public void Stop()
    {
        _mediaController.Stop();
    }

    [ICommand]
    public void Next()
    {
        _mediaController.Next();
    }

    [ICommand]
    public void Previous()
    {
        _mediaController.Previous();
    }

    [ICommand]
    public void Shuffle()
    {
        _mediaController.Shuffle();
    }
    #endregion
}