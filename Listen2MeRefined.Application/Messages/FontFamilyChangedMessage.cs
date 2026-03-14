using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public class FontFamilyChangedMessage(string fontFamily) : ValueChangedMessage<string>(fontFamily);