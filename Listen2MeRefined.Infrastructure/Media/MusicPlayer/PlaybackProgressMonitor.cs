namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public sealed class PlaybackProgressMonitor : IPlaybackProgressMonitor
{
    private const int TimeCheckInterval = 500;
    private double _previousTimeStamp = -1;
    private double _unpausedFor;

    public void Reset()
    {
        _previousTimeStamp = -1;
        _unpausedFor = 0;
    }

    public bool ShouldAdvance(TimeSpan currentTime, TimeSpan totalTime, bool isPlaying)
    {
        if (!isPlaying)
        {
            _previousTimeStamp = currentTime.TotalMilliseconds;
            return false;
        }

        var shouldAdvance = Math.Abs(currentTime.TotalMilliseconds - _previousTimeStamp) < 0.1
                            && _previousTimeStamp >= TimeCheckInterval
                            && _unpausedFor > TimeCheckInterval
                            && currentTime.TotalMilliseconds > totalTime.TotalMilliseconds - 1000;

        _previousTimeStamp = currentTime.TotalMilliseconds;
        _unpausedFor += TimeCheckInterval;

        return shouldAdvance;
    }
}
