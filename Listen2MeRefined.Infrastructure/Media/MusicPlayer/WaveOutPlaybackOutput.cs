using NAudio;
using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public sealed class WaveOutPlaybackOutput : IPlaybackOutput
{
    private WaveOutEvent? _waveOut;

    public float Volume
    {
        get => _waveOut?.Volume ?? 1f;
        set
        {
            if (_waveOut is not null)
            {
                _waveOut.Volume = value;
            }
        }
    }

    public void Play() => _waveOut?.Play();

    public void Pause() => _waveOut?.Pause();

    public void Stop() => _waveOut?.Stop();

    public PlaybackOutputReconfigureResult Reinitialize(WaveStream reader, int outputDeviceIndex)
    {
        var previous = _waveOut;
        WaveOutEvent? candidate = null;
        try
        {
            candidate = new WaveOutEvent
            {
                DeviceNumber = outputDeviceIndex,
                Volume = previous?.Volume ?? 1f
            };
            candidate.Init(reader);

            _waveOut = candidate;
            previous?.Dispose();

            return new PlaybackOutputReconfigureResult(true, PreservedPreviousOutput: false);
        }
        catch (ArgumentOutOfRangeException e)
        {
            candidate?.Dispose();
            return new PlaybackOutputReconfigureResult(false, previous is not null, e, "Device index is out of range");
        }
        catch (MmException e)
        {
            candidate?.Dispose();
            return new PlaybackOutputReconfigureResult(false, previous is not null, e, "Audio device initialization failed");
        }
        catch (InvalidOperationException e)
        {
            candidate?.Dispose();
            return new PlaybackOutputReconfigureResult(false, previous is not null, e, "Playback output initialization failed");
        }
    }

    public void Dispose()
    {
        _waveOut?.Dispose();
    }
}
