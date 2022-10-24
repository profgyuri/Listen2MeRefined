using MediatR;

namespace Listen2MeRefined.Infrastructure.Notifications;

public sealed class FontFamilyChangedNotification : INotification
{
    public FontFamilyChangedNotification(string fontFamily)
    {
        FontFamily = fontFamily;
    }
    
    public string FontFamily { get; }
}