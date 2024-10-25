namespace Listen2MeRefined.Infrastructure.Data.Models;
using System.ComponentModel.DataAnnotations;

public abstract class Model
{
    [Key] public int Id { get; init; }
}