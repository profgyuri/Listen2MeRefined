namespace Listen2MeRefined.Infrastructure.Media.MusicPlayer;

public sealed record PlaybackOutputReconfigureResult(
    bool IsSuccess,
    bool PreservedPreviousOutput,
    Exception? Exception = null,
    string? Context = null);
