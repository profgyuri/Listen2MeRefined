using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Microsoft.Extensions.Options;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public sealed class MainShellViewModel : ShellViewModelBase
{
    private readonly NavigationOptions _navigationOptions;
    
    public MainShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        IShellContextFactory context, 
        IOptions<NavigationOptions> navigationOptions) : base(errorHandler, logger, messenger, context.Create())
    {
        _navigationOptions = navigationOptions.Value;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await NavigationService.NavigateAsync(_navigationOptions.DefaultRoute, cancellationToken: cancellationToken).ConfigureAwait(true);
        
        await base.InitializeAsync(cancellationToken);
    }
}