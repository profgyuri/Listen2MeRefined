﻿namespace Listen2MeRefined.Infrastructure.Data.Models;
using System.ComponentModel.DataAnnotations;

public class PlaylistModel : Model
{
    [Required]
    [MinLength(2)]
    [MaxLength(50)]
    public string? Name { get; set; }

    public List<AudioModel> Songs { get; set; } = new();
}