namespace Listen2MeRefined.Application.ErrorHandling;

/// <summary>
/// Carries structured metadata for unhandled exception reporting.
/// </summary>
/// <param name="Source">Where the unhandled exception originated.</param>
/// <param name="IsTerminating">Whether the caller intends to terminate the process.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the exception was observed.</param>
/// <param name="Context">An optional free-form context label.</param>
public sealed record UnhandledErrorContext(
    UnhandledErrorSource Source,
    bool IsTerminating,
    DateTimeOffset OccurredAtUtc,
    string Context = "");
