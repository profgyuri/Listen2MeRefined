using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public class PopupShellViewModel : ShellViewModelBase
{
    public PopupShellViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IShellContextFactory context) : base(errorHandler, logger, messenger, context.Create())
    {
    }

    public Task NavigateToAsync<TPopupViewModel>(CancellationToken cancellationToken = default)
        where TPopupViewModel : PopupViewModelBase
    {
        return NavigationService.NavigateAsync<TPopupViewModel>(cancellationToken);
    }
}
