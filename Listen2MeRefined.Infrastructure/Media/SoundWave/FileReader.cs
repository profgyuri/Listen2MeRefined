using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Infrastructure.Media.MusicPlayer;
using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class FileReader
    : IFileReader<ISampleProvider>
{
    private readonly ITrackLoader _trackLoader;
    private WaveStream? _waveStream;

    public FileReader(ITrackLoader trackLoader)
    {
        _trackLoader = trackLoader;
    }
    
    public ISampleProvider? SampleProvider { get; private set; }
    
    public int SamplesPerPeak { get; private set; }

    public void Open(string fileName)
    {
        _waveStream?.Dispose();
        _waveStream = null;
        SampleProvider = null;
        SamplesPerPeak = 0;

        var loadResult = _trackLoader.Load(new AudioModel { Path = fileName });
        if (!loadResult.IsSuccess || loadResult.Reader is null)
        {
            throw new InvalidOperationException(
                $"Could not open audio file '{fileName}'. Status: {loadResult.Status}. Reason: {loadResult.Reason}");
        }

        _waveStream = loadResult.Reader;
        SampleProvider = _waveStream.ToSampleProvider();
    }

    public void SetSampleCount(int sampleCount)
    {
        if (_waveStream is null || sampleCount <= 0)
        {
            SamplesPerPeak = 0;
            return;
        }
        
        var bytesPerSample = _waveStream.WaveFormat.BitsPerSample / 8;
        if (bytesPerSample <= 0)
        {
            SamplesPerPeak = 0;
            return;
        }

        var samples = _waveStream.Length / bytesPerSample;
        var samplesNeeded = (int)(samples / sampleCount);
        SamplesPerPeak = samplesNeeded - samplesNeeded % _waveStream.WaveFormat.BlockAlign;
    }
}
