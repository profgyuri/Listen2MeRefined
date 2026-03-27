using System.ComponentModel.DataAnnotations;

namespace Listen2MeRefined.Core.Models;

public abstract class ModelBase
{
    [Key] public int Id { get; init; }
}