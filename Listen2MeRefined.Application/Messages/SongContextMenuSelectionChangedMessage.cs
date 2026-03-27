using CommunityToolkit.Mvvm.Messaging.Messages;
using Listen2MeRefined.Application.ViewModels;

namespace Listen2MeRefined.Application.Messages;

public sealed class SongContextMenuSelectionChangedMessage(ViewModelBase sourceViewModel)
    : ValueChangedMessage<ViewModelBase>(sourceViewModel);
