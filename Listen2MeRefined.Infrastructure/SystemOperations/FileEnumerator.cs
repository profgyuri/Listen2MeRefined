namespace Listen2MeRefined.Infrastructure.SystemOperations;

public sealed class FileEnumerator : IFileEnumerator
{
    private static void ThrowIfNotDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new ArgumentException($"{path} is not a directory");
        }
    }

    private static IEnumerable<string> GetSupportedFiles(string path)
    {
        return Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(file => GlobalConstants.SupportedExtensions.Contains(Path.GetExtension(file)));
    }

    /// <inheritdoc />
    public IEnumerable<string> EnumerateFiles(string path)
    {
        ThrowIfNotDirectory(path);

        return GetSupportedFiles(path);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> EnumerateFilesAsync(string path)
    {
        ThrowIfNotDirectory(path);

        return await Task.Run(() => GetSupportedFiles(path));
    }
}