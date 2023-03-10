namespace Listen2MeRefined.Infrastructure.SystemOperations;

using System.IO.Enumeration;

public sealed class FileEnumerator : IFileEnumerator
{
    private readonly ILogger _logger;

    public FileEnumerator(ILogger logger)
    {
        _logger = logger;
    }

    private static void ThrowIfNotDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new ArgumentException($"{path} is not a directory");
        }
    }

    private IEnumerable<string> GetSupportedFiles(string path)
    {
        var result = new List<string>();
        try
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = false,
                IgnoreInaccessible = true
            };
            var enumerator = new FileSystemEnumerable<string>(path, (ref FileSystemEntry entry) => entry.ToFullPath(), options);
            {
                foreach (var file in enumerator)
                {
                    if (GlobalConstants.SupportedExtensions.Contains(Path.GetExtension(file)))
                    {
                        result.Add(file);
                    }
                }
            }
        }
        catch (UnauthorizedAccessException uae)
        {
            _logger.Error(uae.Message);
        }

        return result;
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