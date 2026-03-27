using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Messages;

public sealed class SearchResultsUpdatedMessage(IReadOnlyList<AudioModel> results)
    : ValueChangedMessage<IReadOnlyList<AudioModel>>(results);
