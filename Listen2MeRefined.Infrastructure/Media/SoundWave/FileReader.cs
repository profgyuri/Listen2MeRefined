using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class FileReader
    : IFileReader<ISampleProvider>
{
    private AudioFileReader _audioFileReader;
    
    /// <inheritdoc/>
    public ISampleProvider SampleProvider { get; private set; }
    
    /// <inheritdoc/>
    public int SamplesPerPeak { get; private set; }

    /// <inheritdoc/>
    public void Open(string fileName)
    {
        _audioFileReader = new AudioFileReader(fileName);
        SampleProvider = _audioFileReader.ToSampleProvider();
    }

    /// <inheritdoc/>
    public void SetSampleCount(int sampleCount)
    {
        if (_audioFileReader is null)
        {
            return;
        }
        
        var bytesPerSample = _audioFileReader.WaveFormat.BitsPerSample / 8;
        var samples = _audioFileReader.Length / bytesPerSample;
        var samplesNeeded = (int)(samples / sampleCount);
        SamplesPerPeak = samplesNeeded - samplesNeeded % _audioFileReader.WaveFormat.BlockAlign;
    }
}