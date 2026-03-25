namespace Listen2MeRefined.Application.ErrorHandling;

/// <summary>
/// Describes where an unhandled exception originated.
/// </summary>
public enum UnhandledErrorSource
{
    Dispatcher,
    AppDomain,
    TaskScheduler,
    Startup,
    WindowInitialization
}
