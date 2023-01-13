using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public sealed class PeakProvider
    : IPeakProvider<ISampleProvider>
{
    private ISampleProvider _sampleProvider;
    private float[] _buffer;
    private const int BlockSize = 200;
    
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

    /// <inheritdoc />
    public async Task<float[]> GetAllPeaksAsync(int count)
    {
        var peaks = await Task.Run(() => GetAllPeaks(count));
        return peaks;
    }

    /// <inheritdoc />
    public void SetReader(IFileReader<ISampleProvider> reader)
    {
        _sampleProvider = reader.SampleProvider;
        _buffer = new float[reader.SamplesPerPeak];
    }
}