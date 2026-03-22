using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public abstract class PopupShellViewModel : ShellViewModelBase
{
    protected PopupShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        ShellContext shellContext) : base(errorHandler, logger, messenger, shellContext)
    {
    }
}