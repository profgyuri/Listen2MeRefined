using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Listen2MeRefined.Application.Navigation.Windows;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public class SettingsShellViewModel : ShellViewModelBase
{
    private readonly IWindowManager _windowManager;
    
    public IReadOnlyList<SettingsShellNavigationItem> NavigationItems { get; }

    public SettingsShellViewModel(
        IErrorHandler errorHandler, 
        ILogger logger, 
        IMessenger messenger, 
        IShellContextFactory context,
        ISettingsShellNavigationProvider navigationProvider, 
        IWindowManager windowManager) : base(errorHandler, logger, messenger, context.Create())
    {
        _windowManager = windowManager;
        NavigationItems = navigationProvider.CreateNavigationItems();

        PropertyChanged += OnPropertyChanged;
        UpdateActiveRoute(CurrentRoute);
    }
    
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await NavigationService.NavigateAsync("settings/general", cancellationToken: cancellationToken).ConfigureAwait(true);
        
        await base.InitializeAsync(cancellationToken);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentRoute))
        {
            UpdateActiveRoute(CurrentRoute);
        }
    }

    private void UpdateActiveRoute(string route)
    {
        foreach (var navigationItem in NavigationItems)
        {
            navigationItem.IsActive = string.Equals(navigationItem.Route, route, StringComparison.OrdinalIgnoreCase);
        }
    }
}
