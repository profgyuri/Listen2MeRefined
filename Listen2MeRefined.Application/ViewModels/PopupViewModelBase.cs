using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels;

public abstract class PopupViewModelBase : ViewModelBase
{
    public virtual string DisplayTitle { get; set; } = string.Empty;

    public virtual string PrimaryButtonText => "Confirm";

    public virtual string SecondaryButtonText => "Cancel";

    protected PopupViewModelBase(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger) : base(errorHandler, logger, messenger)
    { }

    public virtual void SendConfirmedMessage()
    { }

    public virtual void SendCanceledMessage()
    { }
}
