using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.ViewModels.Shells;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels;

public abstract class PopupViewModelBase : ViewModelBase
{
    virtual public string DisplayTitle { get; set; }
    
    protected PopupViewModelBase(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger) : base(errorHandler, logger, messenger)
    { }

    public virtual void SendAcceptedMessage()
    {
        
    }

    public virtual void SendDeniedMessage()
    {
        
    }
}