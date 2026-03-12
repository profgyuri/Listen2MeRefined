using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public class SettingsShellViewModel : ShellViewModelBase
{
    public SettingsShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        IShellContextFactory context) : base(errorHandler, logger, messenger, context.Create())
    {
    }
    
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await NavigationService.NavigateAsync("general", cancellationToken: cancellationToken).ConfigureAwait(true);
        
        await base.InitializeAsync(cancellationToken);
    }
}