namespace Listen2MeRefined.WPF.Utils;

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