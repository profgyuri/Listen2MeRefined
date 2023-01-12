using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class PeakProvider
{
    private readonly ISampleProvider _sampleProvider;
    private readonly float[] _buffer;
    private const int BlockSize = 200;
    
    /// <summary>
    /// Provides peak values for a given sample provider.
    /// </summary>
    /// <param name="stream">Object used for reading audio files.</param>
    public PeakProvider(FileReader stream)
    {
        _sampleProvider = stream.SampleProvider;
        _buffer = new float[stream.SamplesPerPeak];
    }
    
    /// <summary>
    /// Gets the next peak value from the sample provider.
    /// </summary>
    /// <returns>Next peak value between 0 and 1.</returns>
    public float GetNextPeak()
    {
        var max = 0.0f;
        var samplesRead = _sampleProvider.Read(_buffer, 0, _buffer.Length);
        for (var i = 0; i < samplesRead; i += BlockSize)
        {
            var total = 0.0;
            for (var y = 0; y < BlockSize && i + y < samplesRead; y++)
            {
                total += _buffer[i + y] * _buffer[i + y];
            }
            var rms = (float) Math.Sqrt(total/BlockSize);

            max = Math.Max(max, rms);
        }
            
        return max;
    }
    
    /// <summary>
    /// Gets the all peak values from the sample provider.
    /// </summary>
    /// <param name="count">Number of peak values to get.</param>
    /// <returns>Array of peak values between 0 and 1.</returns>
    public float[] GetAllPeaks(int count)
    {
        var peaks = new float[count];
        for (var i = 0; i < count; i++)
        {
            peaks[i] = GetNextPeak();
        }
        return peaks;
    }
}