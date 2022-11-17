using System.ComponentModel.DataAnnotations.Schema;
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
    public string? Path { get; set; }

    #region Overrides of Object
    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        // only true when the 2 paths are the same
        return obj is AudioModel audio &&
               string.Equals(Path, audio.Path, StringComparison.OrdinalIgnoreCase);
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