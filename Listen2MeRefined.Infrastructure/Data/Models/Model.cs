using System.ComponentModel.DataAnnotations;

namespace Listen2MeRefined.Infrastructure.Data.Models;

public abstract class Model
{
    [Key] public int Id { get; init; }
}