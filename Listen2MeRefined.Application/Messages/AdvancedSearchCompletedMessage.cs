using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Listen2MeRefined.Application.Messages;

public sealed class AdvancedSearchCompletedMessage(int resultCount)
    : ValueChangedMessage<int>(resultCount);
