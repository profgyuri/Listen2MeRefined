namespace Listen2MeRefined.Infrastructure.Media;

/// <summary>
/// Needed implementation to control 1 playlist from multiple classes.
/// </summary>
public interface IPlaylistStore
{
    event EventHandler? Changed;

    /// <summary>
    /// Get a snapshot of current items.
    /// </summary>
    IReadOnlyList<AudioModel> Snapshot();

    /// <summary>
    /// Replaces the playlist items with the given ones.
    /// </summary>
    /// <param name="items"></param>
    void ReplaceAll(IEnumerable<AudioModel> items);
    
    /// <summary>
    /// Adds the given items to the playlist.
    /// </summary>
    /// <param name="items">The items to add.</param>
    void AddRange(IEnumerable<AudioModel> items);

    /// <summary>
    /// Removes the item with the given path.
    /// </summary>
    /// <param name="path">The path of the item to remove.</param>
    /// <returns>Whether the item was found and removed.</returns>
    bool RemoveByPath(string path);

    /// <summary>
    /// Moves an item to a new index if present.
    /// </summary>
    bool MoveByPath(string path, int newIndex);

    /// <summary>
    /// Clears the playlist.
    /// </summary>
    void Clear();

    /// <summary>
    /// Shuffles the playlist.
    /// </summary>
    /// <param name="keepFirstByPath">Whether to keep the first item in the first place or not.</param>
    /// <param name="firstPath"></param>
    void Shuffle(bool keepFirstByPath = true, string? firstPath = null);
}