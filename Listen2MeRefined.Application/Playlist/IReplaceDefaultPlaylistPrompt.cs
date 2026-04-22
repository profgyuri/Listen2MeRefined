namespace Listen2MeRefined.Application.Playlist;

/// <summary>
/// Prompts the user to confirm replacement of the current default-playlist contents with imported tracks.
/// </summary>
public interface IReplaceDefaultPlaylistPrompt
{
    /// <summary>
    /// Shows a modal confirmation dialog summarising current vs. imported track counts.
    /// </summary>
    /// <returns><see langword="true"/> when the user confirms the replacement; otherwise <see langword="false"/>.</returns>
    Task<bool> ConfirmReplaceAsync(int existingCount, int importedCount, CancellationToken ct = default);
}
