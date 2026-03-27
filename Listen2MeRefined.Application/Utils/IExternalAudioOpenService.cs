using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Utils;

public interface IExternalAudioOpenService
{
    /// <summary>
    /// Processes file paths received from shell-open and updates the in-memory playlist/player state.
    /// </summary>
    /// <param name="candidatePaths">Candidate file paths forwarded by startup or the single-instance bridge.</param>
    /// <param name="ct">Cancellation token.</param>
    Task OpenAsync(IReadOnlyList<string> candidatePaths, CancellationToken ct = default);

    /// <summary>
    /// Updates the currently loaded song context used when choosing the insertion position for newly opened files.
    /// </summary>
    /// <param name="audio">Current song, or <see langword="null"/> when no song is loaded.</param>
    void SetCurrentSong(AudioModel? audio);
}
