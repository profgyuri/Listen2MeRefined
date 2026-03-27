namespace Listen2MeRefined.Core.Repositories;

public interface IFromFolderRemover
{
    Task RemoveFromFolderAsync(string folderPath);
}