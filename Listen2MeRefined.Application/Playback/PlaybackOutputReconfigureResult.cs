namespace Listen2MeRefined.Application.Playback;

public sealed record PlaybackOutputReconfigureResult(
    bool IsSuccess,
    bool PreservedPreviousOutput,
    Exception? Exception = null,
    string? Context = null);
