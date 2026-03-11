using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public class AdvancedSearchShellViewModel : ShellViewModelBase
{
    public AdvancedSearchShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        IShellContextFactory context) : base(errorHandler, logger, messenger, context.Create())
    {
    }
}