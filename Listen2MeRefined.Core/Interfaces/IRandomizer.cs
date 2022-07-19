namespace Listen2MeRefined.Core.Interfaces;

/// <summary>
///    Abstraction for randomizing collections.
/// </summary>
public interface IRandomizer
{
    /// <summary>
    ///     Randomize a cpredefined collection without a return value.
    /// </summary>
    void Shuffle();
}