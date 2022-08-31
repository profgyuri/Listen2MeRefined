using System.Windows.Media;
using MediatR;

namespace Listen2MeRefined.Infrastructure.Notifications;

public class FontFamilyChangedNotification : INotification
{
    public FontFamilyChangedNotification(FontFamily fontFamily)
    {
        FontFamily = fontFamily;
    }
    
    public FontFamily FontFamily { get; }
}