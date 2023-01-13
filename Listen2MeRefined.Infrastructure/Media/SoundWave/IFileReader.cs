using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public interface IFileReader
{
    /// <summary>
    /// Object for reading the audio file.
    /// </summary>
    ISampleProvider SampleProvider { get; }

    /// <summary>
    /// Gets how many samples are used for 1 pixel in the waveform.
    /// </summary>
    int SamplesPerPeak { get; }
    
    /// <summary>
    /// Opens a file for reading.
    /// </summary>
    /// <param name="fileName">Path to the file.</param>
    void Open(string fileName);
    
    /// <summary>
    /// Sets the number of samples to use for the drawing of the soundwave. Call <see cref="Open"/> before using this.
    /// </summary>
    /// <param name="sampleCount">Width of the soundwave.</param>
    void SetSampleCount(int sampleCount);
}