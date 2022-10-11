namespace Listen2MeRefined.Infrastructure.Mvvm;

[INotifyPropertyChanged]
public partial class NewSongWindowViewModel
{
    [ObservableProperty] private AudioModel _song;

    public NewSongWindowViewModel()
    {
        Song = new AudioModel
        {
            Artist = "W&W",
            Title = "Bigfoot",
            Genre = "House",
            Bitrate = 320,
            BPM = 128
        };
    }
}