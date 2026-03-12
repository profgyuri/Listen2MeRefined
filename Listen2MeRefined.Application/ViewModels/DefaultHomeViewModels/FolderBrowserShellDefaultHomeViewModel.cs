using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;

public class FolderBrowserShellDefaultHomeViewModel : ViewModelBase
{
    public FolderBrowserShellDefaultHomeViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger) : base(errorHandler, logger, messenger)
    {
    }
}