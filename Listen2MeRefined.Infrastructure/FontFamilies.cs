namespace Listen2MeRefined.Infrastructure;

public sealed class FontFamilies
{
    public List<string> FontFamilyNames { get; }

    public FontFamilies(IEnumerable<string> fontFamilies)
    {
        FontFamilyNames = fontFamilies.ToList();
    }
}