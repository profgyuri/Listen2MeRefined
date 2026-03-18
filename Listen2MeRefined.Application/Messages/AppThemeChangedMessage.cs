using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public class AppThemeChangedMessage(string? value = null) : ValueChangedMessage<string?>(value);