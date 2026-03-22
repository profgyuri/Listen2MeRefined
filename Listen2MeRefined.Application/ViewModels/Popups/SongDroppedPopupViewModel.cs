using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Popups;

public class SongDroppedPopupViewModel : PopupViewModelBase
{
    public SongDroppedPopupViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger) : base(errorHandler, logger, messenger)
    { }

    public override string DisplayTitle => "Song dropped";
    
    public override void SendAcceptedMessage()
    {
        //Messenger.Send()
    }
}