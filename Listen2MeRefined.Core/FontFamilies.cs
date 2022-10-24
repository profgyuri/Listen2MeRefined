namespace Listen2MeRefined.Core;

public class FontFamilies
{
    public List<string> FontFamilyNames { get; }

    public FontFamilies(IEnumerable<string> fontFamilies)
    {
        FontFamilyNames = fontFamilies.ToList();
    }
}