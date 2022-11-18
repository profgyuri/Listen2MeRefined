﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Listen2MeRefined.Core.Models;

[Table("Songs")]
public sealed class AudioModel : Model
{
    public string? Artist { get; set; }

    public string Title { get; set; }

    public string? Genre { get; set; }

    public short BPM { get; set; }

    public short Bitrate { get; set; }

    [NotMapped]
    public string Display =>
        string.IsNullOrEmpty(Artist)
            ? $"{Path?.Split(System.IO.Path.PathSeparator)[^1] ?? ""}"
            : string.Join(" - ", Artist, Title);

    public TimeSpan Length { get; set; }
    public string? Path { get; init; }
    
    /// <summary>
    /// Updates the properties of this instance with the properties of the given instance.
    /// </summary>
    /// <param name="from">The instance to copy the properties from.</param>
    /// <returns></returns>
    public AudioModel Update(AudioModel from)
    {
        Artist = from.Artist;
        Title = from.Title;
        Genre = from.Genre;
        BPM = from.BPM;
        Bitrate = from.Bitrate;
        Length = from.Length;
        return this;
    }

    #region Overrides of Object
    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        // only true when the 2 paths are the same
        return obj is AudioModel audio &&
               string.Equals(Path, audio.Path, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Path?.GetHashCode() ?? -1;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine($"  Artist: {Artist};");
        builder.AppendLine($"  Title: {Title};");
        builder.AppendLine($"  Genre: {Genre};");
        builder.AppendLine($"  Bpm: {BPM};");
        builder.AppendLine("}");
        return builder.ToString();
    }
    #endregion
}