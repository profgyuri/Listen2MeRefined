namespace Listen2MeRefined.Core.Interfaces;

public interface IGlobalHook
{
    /// <summary>
    /// Sets up the keys used by the application.
    /// </summary>
    public void Register();
    
    /// <summary>
    /// Cleans up the used resources.
    /// </summary>
    public void Unregister();
}