namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

public interface IFromFolderRemover
{
    Task RemoveFromFolderAsync(string folderPath);
}