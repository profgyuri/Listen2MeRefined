using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.ViewModels.DefaultHomeViewModels;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public class FolderBrowserShellViewModel : ShellViewModelBase
{
    public FolderBrowserShellViewModel(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        IShellContextFactory context) : base(errorHandler, logger, messenger, context.Create())
    {
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await NavigationService.NavigateAsync<FolderBrowserShellDefaultHomeViewModel>(cancellationToken).ConfigureAwait(true);

        await base.InitializeAsync(cancellationToken);
    }
}
