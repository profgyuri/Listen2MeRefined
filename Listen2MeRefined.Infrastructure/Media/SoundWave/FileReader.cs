using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class FileReader
{
    /// <summary>
    /// Object for reading the audio file.
    /// </summary>
    public ISampleProvider SampleProvider { get; }
    
    /// <summary>
    /// Gets how many samples are used for 1 pixel in the waveform.
    /// </summary>
    public int SamplesPerPeak { get; }

    /// <summary>
    /// Wrapper class to keep track of the samples per peak and the sample provider.
    /// </summary>
    /// <param name="path">The path to the audio file.</param>
    /// <param name="width">The width of the waveform in pixels.</param>
    public FileReader(string path, int width)
    {
        var waveStream = new AudioFileReader(path);
        var bytesPerSample = waveStream.WaveFormat.BitsPerSample / 8;
        var samples = waveStream.Length / bytesPerSample;
        var samplesNeeded = (int)(samples / width);
        SampleProvider = waveStream.ToSampleProvider();
        SamplesPerPeak = samplesNeeded - samplesNeeded % waveStream.WaveFormat.BlockAlign;
    }
}