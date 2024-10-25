namespace Listen2MeRefined.Infrastructure;

public interface IGlobalHook
{
    /// <summary>
    ///     Sets up the keys used by the application.
    /// </summary>
    public Task RegisterAsync();

    /// <summary>
    ///     Cleans up the used resources.
    /// </summary>
    public void Unregister();
}