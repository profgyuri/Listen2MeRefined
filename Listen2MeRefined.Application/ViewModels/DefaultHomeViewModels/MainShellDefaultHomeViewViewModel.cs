using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;

public class MainShellDefaultHomeViewViewModel : ViewModelBase
{
    public MainShellDefaultHomeViewViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger) : base(errorHandler, logger, messenger)
    {
    }
}