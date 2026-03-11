using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public class CornerWindowShellViewModel : ShellViewModelBase
{
    public CornerWindowShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        INavigationService navigationService, 
        NavigationState navigationState) : base(errorHandler, logger, messenger, navigationService, navigationState)
    {
    }
}