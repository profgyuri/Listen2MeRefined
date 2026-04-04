using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Application.ViewModels.ContextMenus;

namespace Listen2MeRefined.Application.Messages;

public sealed class SongContextMenuSelectionChangedMessage(ISongContextMenuHost sourceHost)
    : ValueChangedMessage<ISongContextMenuHost>(sourceHost);
