namespace Listen2MeRefined.Infrastructure.Notifications;
using MediatR;

public sealed class FontFamilyChangedNotification : INotification
{
    public string FontFamily { get; }

    public FontFamilyChangedNotification(string fontFamily)
    {
        FontFamily = fontFamily;
    }
}