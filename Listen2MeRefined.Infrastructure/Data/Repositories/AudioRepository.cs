using System.Text;
using Dapper;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Listen2MeRefined.Infrastructure.Data.Repositories;

public sealed class AudioRepository : 
    RepositoryBase<AudioModel>,
    IAudioRepository
{
    public AudioRepository(
        IDbContextFactory<DataContext> dataContextFactory,
        IDbConnection dbConnection,
        ILogger logger)
        : base(logger, dataContextFactory, dbConnection)
    { }

    public async Task<IEnumerable<AudioModel>> ReadAsync(IEnumerable<AdvancedFilter> criterias, bool matchAll)
    {
        var filters = criterias as AdvancedFilter[] ?? criterias.ToArray();
        if (filters.Length == 0)
        {
            return Enumerable.Empty<AudioModel>();
        }
        
        var validFields = typeof(AudioModel)
            .GetProperties()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        var parameters = new DynamicParameters();
        var clauses = new List<string>(filters.Length);

        for (var i = 0; i < filters.Length; i++)
        {
            var filter = filters[i];
            if (!validFields.Contains(filter.Field))
            {
                throw new InvalidOperationException($"Unsupported filter field: {filter.Field}");
            }

            var parameterName = $"p{i}";
            var sqlOperator = GetSqlOperator(filter.Operator);
            var value = filter.Operator is AdvancedFilterOperator.Contains or AdvancedFilterOperator.NotContains
                ? $"%{filter.Value}%"
                : filter.Value;

            parameters.Add(parameterName, value);
            clauses.Add($"{filter.Field} {sqlOperator} @{parameterName} COLLATE NOCASE");
        }

        var joiner = matchAll ? " AND " : " OR ";
        var whereClause = string.Join(joiner, clauses);
        var query = new StringBuilder($"SELECT * FROM {_tableName} WHERE ")
            .Append(whereClause)
            .ToString();

        return await _dbConnection.QueryAsync<AudioModel>(query, parameters);
        
        static string GetSqlOperator(AdvancedFilterOperator op)
        {
            return op switch
            {
                AdvancedFilterOperator.Equal => "=",
                AdvancedFilterOperator.NotEqual => "<>",
                AdvancedFilterOperator.Contains => "LIKE",
                AdvancedFilterOperator.NotContains => "NOT LIKE",
                AdvancedFilterOperator.GreaterThan => ">",
                AdvancedFilterOperator.LessThan => "<",
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported advanced filter operator")
            };
        }
    }

    public async Task<AudioModel?> ReadByPathAsync(string path)
    {
        const string query = "SELECT * FROM Songs WHERE Path = @Path COLLATE NOCASE LIMIT 1";
        return await _dbConnection.QueryFirstOrDefaultAsync<AudioModel>(query, new { Path = path });
    }

    public async Task<IReadOnlyList<AudioModel>> ReadByFolderScopeAsync(string folderPath, bool includeSubdirectories)
    {
        var normalizedFolder = NormalizeFolder(folderPath);
        var pathPrefixBackslash = $"{normalizedFolder}\\%";
        var pathPrefixSlash = $"{normalizedFolder}/%";
        const string query = """
            SELECT *
            FROM Songs
            WHERE Path IS NOT NULL
              AND (
                   Path = @Folder
                   OR Path LIKE @PrefixBackslash COLLATE NOCASE
                   OR Path LIKE @PrefixSlash COLLATE NOCASE
              )
            """;

        var candidates = (await _dbConnection.QueryAsync<AudioModel>(query, new
        {
            Folder = normalizedFolder,
            PrefixBackslash = pathPrefixBackslash,
            PrefixSlash = pathPrefixSlash
        })).ToArray();

        if (includeSubdirectories)
        {
            return candidates
                .Where(x => IsInSubtree(x.Path, normalizedFolder))
                .ToArray();
        }

        return candidates
            .Where(x => IsTopLevelFile(x.Path, normalizedFolder))
            .ToArray();
    }

    public async Task PersistScanChangesAsync(
        IReadOnlyCollection<AudioModel> toInsert,
        IReadOnlyCollection<AudioModel> toUpdate,
        IReadOnlyCollection<AudioModel> toRemove)
    {
        using var transaction = _dbConnection.BeginTransaction();
        try
        {
            if (toUpdate.Count > 0)
            {
                const string updateSql = """
                    UPDATE Songs
                    SET Artist = @Artist,
                        Title = @Title,
                        Genre = @Genre,
                        BPM = @BPM,
                        Bitrate = @Bitrate,
                        Length = @Length,
                        LastWriteUtc = @LastWriteUtc,
                        LengthBytes = @LengthBytes
                    WHERE Id = @Id
                    """;
                await _dbConnection.ExecuteAsync(updateSql, toUpdate, transaction);
            }

            if (toInsert.Count > 0)
            {
                const string insertSql = """
                    INSERT OR IGNORE INTO Songs
                        (Artist, Title, Genre, BPM, Bitrate, Length, Path, LastWriteUtc, LengthBytes)
                    VALUES
                        (@Artist, @Title, @Genre, @BPM, @Bitrate, @Length, @Path, @LastWriteUtc, @LengthBytes)
                    """;
                await _dbConnection.ExecuteAsync(insertSql, toInsert, transaction);
            }

            if (toRemove.Count > 0)
            {
                var ids = toRemove.Select(x => x.Id).Where(id => id > 0).Distinct().ToArray();
                if (ids.Length > 0)
                {
                    const string removePlaylistLinksSql = "DELETE FROM PlaylistSongs WHERE SongId IN @Ids";
                    await _dbConnection.ExecuteAsync(removePlaylistLinksSql, new { Ids = ids }, transaction);

                    const string removeSql = "DELETE FROM Songs WHERE Id IN @Ids";
                    await _dbConnection.ExecuteAsync(removeSql, new { Ids = ids }, transaction);
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task RemoveFromFolderAsync(string folderPath)
    {
        const string removePlaylistLinksSql = """
            DELETE FROM PlaylistSongs
            WHERE SongId IN (
                SELECT Id
                FROM Songs
                WHERE Path LIKE @PathPrefix
            );
            """;
        await _dbConnection.ExecuteAsync(removePlaylistLinksSql, new { PathPrefix = $"{folderPath}%" });

        var sql = $"DELETE FROM {_tableName} WHERE Path LIKE @PathPrefix";
        await _dbConnection.ExecuteAsync(sql, new { PathPrefix = $"{folderPath}%" });
    }

    private static string NormalizeFolder(string folderPath)
    {
        var fullPath = Path.GetFullPath(folderPath.Trim());
        var root = Path.GetPathRoot(fullPath);
        if (!string.IsNullOrWhiteSpace(root)
            && fullPath.Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.TrimEnd(Path.AltDirectorySeparatorChar);
        }

        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsInSubtree(string? filePath, string folderPath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var normalizedFile = filePath.Replace('/', '\\');
        var normalizedFolder = folderPath.Replace('/', '\\');
        if (normalizedFile.Equals(normalizedFolder, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalizedFile.StartsWith($"{normalizedFolder}\\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTopLevelFile(string? filePath, string folderPath)
    {
        if (!IsInSubtree(filePath, folderPath))
        {
            return false;
        }

        var normalizedFile = filePath!.Replace('/', '\\');
        var normalizedFolder = folderPath.Replace('/', '\\');
        var directory = Path.GetDirectoryName(normalizedFile);
        return directory is not null && directory.Equals(normalizedFolder, StringComparison.OrdinalIgnoreCase);
    }
}
