using System.ComponentModel.DataAnnotations;

namespace Listen2MeRefined.Core.Models;

public sealed class PlaylistModel : ModelBase
{
    [Required]
    [MinLength(2)]
    [MaxLength(50)]
    public string? Name { get; set; }

    public bool IsPinned { get; set; }

    public int DisplayOrder { get; set; }

    public List<AudioModel> Songs { get; init; } = new();
}