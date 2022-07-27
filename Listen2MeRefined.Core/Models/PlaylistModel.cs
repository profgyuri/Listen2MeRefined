namespace Listen2MeRefined.Core.Models;

using System.ComponentModel.DataAnnotations;

public class PlaylistModel
{
    [Key]
    public int Id { get; set; }
    [Required, MinLength(2), MaxLength(50)]
    public string? Name { get; set; }
    public List<AudioModel> Songs { get; set; } = new();
}