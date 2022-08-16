namespace Listen2MeRefined.Core.Interfaces;

/// <summary>
///     Contract for controlling any media element.
/// </summary>
public interface IMediaController
{
    /// <summary>
    ///     Play or pause the playback.
    /// </summary>
    void PlayPause();
    
    /// <summary>
    ///     Stop the playback and jump to start.
    /// </summary>
    void Stop();

    /// <summary>
    ///     Jump to the next media element.
    /// </summary>
    void Next();

    /// <summary>
    ///     Jump to the previous media element.
    /// </summary>
    void Previous();

    /// <summary>
    ///     Randomizes the order of the elements.
    /// </summary>
    void Shuffle();
    
    double CurrentTime { get; set; }
    
    float Volume { get; set; }
}