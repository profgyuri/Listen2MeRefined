using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Startup;
using Microsoft.Extensions.Options;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public sealed class MainShellViewModel : ShellViewModelBase
{
    private readonly NavigationOptions _navigationOptions;
    private readonly IWindowManager _windowManager;
    private readonly IStartupManager _startupManager;
    
    public MainShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        IShellContextFactory context, 
        IOptions<NavigationOptions> navigationOptions, 
        IWindowManager windowManager, 
        IStartupManager startupManager) : base(errorHandler, logger, messenger, context.Create())
    {
        _windowManager = windowManager;
        _startupManager = startupManager;
        _navigationOptions = navigationOptions.Value;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _startupManager.StartAsync(cancellationToken);
        
        await NavigationService
            .NavigateAsync(_navigationOptions.DefaultRoute, cancellationToken: cancellationToken)
            .ConfigureAwait(true);
        
        await base.InitializeAsync(cancellationToken);
    }
}