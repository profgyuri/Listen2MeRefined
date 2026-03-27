using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Application.Messages;

public class PlayerStateChangedMessage(PlayerState state) : ValueChangedMessage<PlayerState>(state);