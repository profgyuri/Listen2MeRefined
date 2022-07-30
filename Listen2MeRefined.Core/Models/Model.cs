namespace Listen2MeRefined.Core.Models;

using System.ComponentModel.DataAnnotations;

public abstract class Model
{
    [Key]public int Id { get; init; }
}
