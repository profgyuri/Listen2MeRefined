namespace Listen2MeRefined.Infrastructure.Media;

public class MusicPlayer
{
    private readonly IMediaController _mediaController;
    private readonly IRandomizer _randomizer;

    public MusicPlayer(IMediaController mediaController, IRandomizer randomizer)
    {
        _mediaController = mediaController;
        _randomizer = randomizer;
    }

    public void PlayPause()
    {
        _mediaController.PlayPause();
    }

    public void Shuffle()
    {
        _randomizer.Shuffle();
    }

    public void Stop()
    {
        _mediaController.Stop();
    }

    public void Next()
    {
        _mediaController.Next();
    }

    public void Previous()
    {
        _mediaController.Previous();
    }
}