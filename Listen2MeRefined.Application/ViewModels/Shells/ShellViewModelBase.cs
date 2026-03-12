using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listen2MeRefined.Application.ErrorHandling;
using Listen2MeRefined.Application.Navigation;
using Serilog;

namespace Listen2MeRefined.Application.ViewModels.Shells;

public abstract partial class ShellViewModelBase : ViewModelBase
{
    protected readonly INavigationService NavigationService;
    private readonly NavigationState _navigationState;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _currentRoute = string.Empty;

    protected ShellViewModelBase(
        IErrorHandler errorHandler,
        ILogger logger,
        IMessenger messenger,
        ShellContext shellContext) : base(errorHandler, logger, messenger)
    {
        NavigationService = shellContext.NavigationService;
        _navigationState = shellContext.NavigationState;
        CurrentRoute = _navigationState.CurrentRoute;
        CurrentViewModel = _navigationState.CurrentViewModel;
        _navigationState.PropertyChanged += OnNavigationStateChanged;
    }

    [RelayCommand]
    private Task NavigateAsync(string route) =>
        ExecuteSafeAsync(
            ct => NavigationService.NavigateAsync(route, cancellationToken: ct),
            $"Navigate({route})");

    private void OnNavigationStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NavigationState.CurrentRoute))
            CurrentRoute = _navigationState.CurrentRoute;

        if (e.PropertyName == nameof(NavigationState.CurrentViewModel))
            CurrentViewModel = _navigationState.CurrentViewModel;
    }
}