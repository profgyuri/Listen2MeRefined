using MediatR;

namespace Listen2MeRefined.Application.Notifications;

public sealed class FontFamilyChangedNotification : INotification
{
    public string FontFamily { get; }

    public FontFamilyChangedNotification(string fontFamily)
    {
        FontFamily = fontFamily;
    }
}