namespace Listen2MeRefined.Application.Navigation;

public sealed class ShellContext
{
    public NavigationState NavigationState { get; }
    public INavigationService NavigationService { get; }
    public IInitializationTracker InitializationTracker { get; }

    public ShellContext(
        NavigationState navigationState,
        INavigationService navigationService,
        IInitializationTracker initializationTracker)
    {
        NavigationState = navigationState;
        NavigationService = navigationService;
        InitializationTracker = initializationTracker;
    }
}