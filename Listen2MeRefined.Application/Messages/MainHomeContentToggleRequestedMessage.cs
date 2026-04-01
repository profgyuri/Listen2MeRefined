using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Application.Messages;

public sealed class MainHomeContentToggleRequestedMessage(MainHomeContentTarget value)
    : ValueChangedMessage<MainHomeContentTarget>(value);
