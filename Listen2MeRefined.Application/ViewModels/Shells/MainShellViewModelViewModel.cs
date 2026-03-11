using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public sealed class MainShellViewModelViewModel : ShellViewModelBase
{
    public MainShellViewModelViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        INavigationService navigationService, 
        NavigationState navigationState) : base(errorHandler, logger, messenger, navigationService, navigationState)
    {
    }
}