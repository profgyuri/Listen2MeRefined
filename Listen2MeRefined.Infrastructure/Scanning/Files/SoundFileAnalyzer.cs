using Ardalis.GuardClauses;
using Listen2MeRefined.Application.Files;
using Listen2MeRefined.Core.Models;
using File = TagLib.File;

namespace Listen2MeRefined.Infrastructure.Scanning.Files;

public sealed class SoundFileAnalyzer : IFileAnalyzer<AudioModel>
{
    public Task<AudioModel> AnalyzeAsync(string path, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            return AnalyzeCore(path);
        }, ct);
    }

    private static AudioModel AnalyzeCore(string path)
    {
        Guard.Against.NotExistingFile(path, nameof(path));

        var info = new FileInfo(path);
        using var file = File.Create(path);

        return new AudioModel
        {
            Path = path,
            Title = file.Tag.Title,
            Artist = string.Join("; ", file.Tag.Performers!),
            Genre = string.Join("; ", file.Tag.Genres!),
            BPM = SanitizeBpm(file.Tag.BeatsPerMinute),
            Bitrate = (short)file.Properties.AudioBitrate,
            Length = file.Properties.Duration,
            LastWriteUtc = info.LastWriteTimeUtc,
            LengthBytes = info.Length
        };
    }

    /// <summary>
    /// Handles BPM values inflated by decimal-format tags (e.g. "150.000000" parsed as 150000000).
    /// Divides by increasing powers of 10 and rounds to recover the real value; returns 0 if unrecoverable.
    /// </summary>
    internal static uint SanitizeBpm(uint bpm)
    {
        const uint maxReasonableBpm = 999;

        if (bpm <= maxReasonableBpm)
            return bpm;

        var divisor = 10u;
        while (bpm / divisor > maxReasonableBpm)
            divisor *= 10;

        return (uint)Math.Round((double)bpm / divisor);
    }
}
