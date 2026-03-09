namespace Listen2MeRefined.Application.Utils;

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