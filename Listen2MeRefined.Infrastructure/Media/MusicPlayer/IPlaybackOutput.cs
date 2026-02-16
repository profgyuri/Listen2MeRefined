using NAudio.Wave;

namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

/// <summary>
/// Controls playback output device state and initialization for the active audio stream.
/// </summary>
public interface IPlaybackOutput : IDisposable
{
    /// <summary>
    /// Gets or sets the output volume in range supported by the underlying output implementation.
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Starts or resumes playback on the configured output.
    /// </summary>
    void Play();

    /// <summary>
    /// Pauses playback while preserving the current stream position.
    /// </summary>
    void Pause();

    /// <summary>
    /// Stops playback on the configured output.
    /// </summary>
    void Stop();

    /// <summary>
    /// Re-initializes the playback output against the provided stream and target device.
    /// </summary>
    /// <param name="reader">The reader to initialize the output with.</param>
    /// <param name="outputDeviceIndex">The audio output device index.</param>
    /// <returns>A typed reconfiguration result describing success and fallback behavior.</returns>
    PlaybackOutputReconfigureResult Reinitialize(WaveStream reader, int outputDeviceIndex);
}
