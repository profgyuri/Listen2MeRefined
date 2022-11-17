using System.ComponentModel.DataAnnotations;

namespace Listen2MeRefined.Core.Models;

public class PlaylistModel : Model
{
    [Required]
    [MinLength(2)]
    [MaxLength(50)]
    public string? Name { get; set; }

    public List<AudioModel> Songs { get; set; } = new();
}