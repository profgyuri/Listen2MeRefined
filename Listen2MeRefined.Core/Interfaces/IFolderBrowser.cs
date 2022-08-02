namespace Listen2MeRefined.Core.Interfaces;

public interface IFolderBrowser
{
    /// <summary>
    ///     Returns every subfolder in <paramref name="path"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    IEnumerable<string> GetSubFolders(string path);

    /// <summary>
    ///     Returns a list of the phisycal drives.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetDrives();
}