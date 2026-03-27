namespace Listen2MeRefined.Application.Utils;

/// <summary>
/// Buffers shell-open path batches until a consumer is ready, and forwards new batches to active consumers.
/// </summary>
public interface IExternalAudioOpenInbox
{
    /// <summary>
    /// Adds a shell-open request to the inbox.
    /// </summary>
    /// <param name="paths">Candidate file paths.</param>
    void Enqueue(IReadOnlyList<string> paths);

    /// <summary>
    /// Registers a consumer for shell-open requests.
    /// </summary>
    /// <param name="consumer">Consumer callback.</param>
    /// <param name="replayPending">When true, pending requests are delivered immediately after registration.</param>
    /// <returns>A token that unregisters the consumer when disposed.</returns>
    IDisposable RegisterConsumer(Action<IReadOnlyList<string>> consumer, bool replayPending = true);
}
