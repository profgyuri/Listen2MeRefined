using Ardalis.GuardClauses;
using File = TagLib.File;

namespace Listen2MeRefined.Infrastructure.Media;

public sealed class MetadataExtractor : IMetadataExtractor<AudioModel>
{
    /// <summary>
    ///     Gets an <see cref="AudioModel" /> after analyzing the file at <paramref name="path" />.
    /// </summary>
    /// <param name="path">Local path to the audio file.</param>
    public AudioModel Extract(string path)
    {
        Guard.Against.NotExistingFile(path, nameof(path));

        var file = File.Create(path);

        return new AudioModel
        {
            Path = path,
            Title = file.Tag.Title,
            Artist = string.Join("; ", file.Tag.Performers!),
            Genre = string.Join("; ", file.Tag.Genres!),
            BPM = (short) file.Tag.BeatsPerMinute,
            Bitrate = (short) file.Properties.AudioBitrate,
            Length = file.Properties.Duration
        };
    }

    /// <summary>
    ///     Gets a list of <see cref="AudioModel" /> objects after analyzing the files at <paramref name="paths" />.
    /// </summary>
    /// <param name="paths">List of local paths leading to audio files.</param>
    public IEnumerable<AudioModel> Extract(IEnumerable<string> paths)
    {
        var result = new List<AudioModel>();

        foreach (var path in paths)
        {
            var audio = Extract(path);

            result.Add(audio);
        }

        return result;
    }

    /// <summary>
    ///     Gets an <see cref="AudioModel" /> after analyzing the file at <paramref name="path" />.
    /// </summary>
    /// <param name="path">Local path to the audio file.</param>
    public async Task<AudioModel> ExtractAsync(string path)
    {
        return await Task.Run(() => Extract(path));
    }

    /// <summary>
    ///     Gets a list of <see cref="AudioModel" /> objects after analyzing the files at <paramref name="paths" />.
    /// </summary>
    /// <param name="paths">List of local paths leading to audio files.</param>
    public async Task<IEnumerable<AudioModel>> ExtractAsync(IEnumerable<string> paths)
    {
        var result = new List<AudioModel>();

        foreach (var path in paths)
        {
            var audio = await ExtractAsync(path);

            result.Add(audio);
        }

        return result;
    }
}