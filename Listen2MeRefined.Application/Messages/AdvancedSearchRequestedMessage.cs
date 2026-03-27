using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Application.Searching;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Enums;

namespace Listen2MeRefined.Application.Messages;

public sealed record AdvancedSearchRequestedMessageData(
    IReadOnlyList<AdvancedFilter> Filters,
    SearchMatchMode MatchMode);

public sealed class AdvancedSearchRequestedMessage(AdvancedSearchRequestedMessageData value)
    : ValueChangedMessage<AdvancedSearchRequestedMessageData>(value);
