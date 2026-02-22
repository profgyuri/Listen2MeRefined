using System.IO.Enumeration;
using System.Runtime.CompilerServices;

namespace Listen2MeRefined.Infrastructure.SystemOperations;

public sealed class FileEnumerator : IFileEnumerator
{
    private readonly ILogger _logger;
    private static readonly HashSet<string> SupportedExtensions = new(
        GlobalConstants.SupportedExtensions,
        StringComparer.OrdinalIgnoreCase);

    public FileEnumerator(ILogger logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<string> EnumerateFilesAsync(
        string path,
        bool includeSubdirectories,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfNotDirectory(path);

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = includeSubdirectories,
            IgnoreInaccessible = true
        };

        FileSystemEnumerable<string>? enumerator = null;
        try
        {
            enumerator = new FileSystemEnumerable<string>(
                path,
                (ref FileSystemEntry entry) => entry.ToFullPath(),
                options);
        }
        catch (UnauthorizedAccessException uae)
        {
            _logger.Error(uae, "[FileEnumerator] Access denied while enumerating files in {Path}", path);
        }

        if (enumerator is null)
        {
            yield break;
        }

        foreach (var file in enumerator)
        {
            ct.ThrowIfCancellationRequested();
            if (SupportedExtensions.Contains(Path.GetExtension(file)))
            {
                yield return file;
            }
        }

        await Task.CompletedTask;
    }

    private static void ThrowIfNotDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new ArgumentException($"{path} is not a directory", nameof(path));
        }
    }
}
