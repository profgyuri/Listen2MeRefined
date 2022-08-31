using System.Windows.Media;

namespace Listen2MeRefined.Infrastructure.Notifications;

public class FontFamilyChangedNotification
{
    public FontFamilyChangedNotification(FontFamily fontFamily)
    {
        FontFamily = fontFamily;
    }
    
    public FontFamily FontFamily { get; }
}