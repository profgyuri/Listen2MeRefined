using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public class AutoFlowTrackTextChangedMessage(bool enabled) : ValueChangedMessage<bool>(enabled);
