namespace Listen2MeRefined.Infrastructure.Mvvm;

public interface IGlobalHook
{
    /// <summary>
    ///     Sets up the keys used by the application.
    /// </summary>
    public void Register();

    /// <summary>
    ///     Cleans up the used resources.
    /// </summary>
    public void Unregister();
}