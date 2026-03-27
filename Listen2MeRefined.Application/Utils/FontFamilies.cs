namespace Listen2MeRefined.Application.Utils;

public sealed class FontFamilies
{
    public List<string> FontFamilyNames { get; }

    public FontFamilies(IEnumerable<string> fontFamilies)
    {
        FontFamilyNames = fontFamilies.ToList();
    }
}