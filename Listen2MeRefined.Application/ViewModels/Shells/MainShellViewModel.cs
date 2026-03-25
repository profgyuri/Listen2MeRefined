using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Listen2MeRefined.Application.Updating;
using Listen2MeRefined.Application.Utils;
using Microsoft.Extensions.Options;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public sealed partial class MainShellViewModel : ShellViewModelBase
{
    private readonly NavigationOptions _navigationOptions;
    private readonly IWindowManager _windowManager;
    private readonly IAppUpdateChecker _appUpdateChecker;
    private readonly IUiDispatcher _ui;
    
    [ObservableProperty] private bool _isUpdateAvailable;
    
    public MainShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        IShellContextFactory context, 
        IOptions<NavigationOptions> navigationOptions, 
        IWindowManager windowManager, 
        IAppUpdateChecker appUpdateChecker, 
        IUiDispatcher ui) : base(errorHandler, logger, messenger, context.Create())
    {
        _windowManager = windowManager;
        _appUpdateChecker = appUpdateChecker;
        _ui = ui;
        _navigationOptions = navigationOptions.Value;
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsUpdateAvailable = (await _appUpdateChecker.CheckForUpdatesAsync()).IsUpdateAvailable;
        
        await NavigationService
            .NavigateAsync(_navigationOptions.DefaultRoute, cancellationToken: cancellationToken)
            .ConfigureAwait(true);
        
        await base.InitializeAsync(cancellationToken);
        
        Logger.Debug("[MainShellViewModel] Finished InitializeAsync");
    }

    [RelayCommand]
    private async Task OpenSettingsWindow()
    {
        await ExecuteSafeAsync(async ct =>
        {
            await _ui.InvokeAsync(OpenSettingsOnUiAsync, ct);
            return;

            async Task OpenSettingsOnUiAsync()
            {
                await _windowManager.ShowWindowAsync<SettingsShellViewModel>(
                    WindowShowOptions.CenteredOnMainWindow(),
                    ct);
            }
        });
    }
}
