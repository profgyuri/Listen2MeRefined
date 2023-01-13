using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.SoundWave;

public interface IPeakProvider
{
    /// <summary>
    /// Gets the next peak value from the sample provider.
    /// </summary>
    /// <returns>Next peak value between 0 and 1.</returns>
    float GetNextPeak();

    /// <summary>
    /// Gets the all peak values from the sample provider.
    /// </summary>
    /// <param name="count">Number of peak values to get.</param>
    /// <returns>Array of peak values between 0 and 1.</returns>
    float[] GetAllPeaks(int count);
    
    /// <summary>
    /// Gets the all peak values from the sample provider.
    /// </summary>
    /// <param name="count">Number of peak values to get.</param>
    Task<float[]> GetAllPeaksAsync(int count);

    void SetReader(IFileReader reader);
}