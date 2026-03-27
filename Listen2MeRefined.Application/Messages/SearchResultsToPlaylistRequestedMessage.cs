using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Application.Messages;

public sealed class SearchResultsToPlaylistRequestedMessage(IReadOnlyList<AudioModel> songs)
    : ValueChangedMessage<IReadOnlyList<AudioModel>>(songs);
