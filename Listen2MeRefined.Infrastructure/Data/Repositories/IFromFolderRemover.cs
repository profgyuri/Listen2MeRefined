namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public interface IFromFolderRemover
{
    Task RemoveFromFolderAsync(string folderPath);
}