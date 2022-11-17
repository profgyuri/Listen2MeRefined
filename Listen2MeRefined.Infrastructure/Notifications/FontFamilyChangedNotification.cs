using MediatR;

namespace Listen2MeRefined.Infrastructure.Notifications;

public sealed class FontFamilyChangedNotification : INotification
{
    public string FontFamily { get; }

    public FontFamilyChangedNotification(string fontFamily)
    {
        FontFamily = fontFamily;
    }
}