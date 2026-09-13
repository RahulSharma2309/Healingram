using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Healingram.BuildingBlocks.Persistence;

public sealed class SchemaInstaller(IConfiguration configuration, ILogger<SchemaInstaller> logger)
{
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");

        var configured = configuration["Schema:SqlPath"] ?? "db/001_schemas.sql";
        var sqlFiles = ResolveSqlFiles(configured);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var sqlPath in sqlFiles)
        {
            var sql = await File.ReadAllTextAsync(sqlPath, cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.CommandTimeout = 60;
            await command.ExecuteNonQueryAsync(cancellationToken);
            logger.LogInformation("Postgres schema script applied from {Path}", sqlPath);
        }
    }

    /// <summary>
    /// Runs every <c>*.sql</c> in the catalog db folder in filename order (001 then 002…).
    /// Files found in output, working directory, or a parent <c>db/</c> folder are unioned by name.
    /// </summary>
    internal static IReadOnlyList<string> ResolveSqlFiles(string configured)
    {
        var byName = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void AddFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var name = Path.GetFileName(path);
            if (!byName.ContainsKey(name))
            {
                byName[name] = path;
            }
        }

        void AddDirectory(string? dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(dir, "*.sql"))
            {
                AddFile(file);
            }
        }

        var primary = TryResolveSqlPath(configured);
        if (primary is not null)
        {
            AddFile(primary);
            AddDirectory(Path.GetDirectoryName(primary));
        }

        foreach (var dir in CandidateSqlDirectories())
        {
            AddDirectory(dir);
        }

        if (byName.Count == 0)
        {
            throw new FileNotFoundException($"Schema file not found: {configured}");
        }

        return byName.Values.ToArray();
    }

    internal static string ResolveSqlPath(string configured)
        => TryResolveSqlPath(configured)
           ?? throw new FileNotFoundException($"Schema file not found: {configured}");

    internal static string? TryResolveSqlPath(string configured)
    {
        if (Path.IsPathRooted(configured) && File.Exists(configured))
        {
            return configured;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, configured),
            Path.Combine(Directory.GetCurrentDirectory(), configured),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "db", "001_schemas.sql"))
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    internal static IEnumerable<string> CandidateSqlDirectories()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "db");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "db");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "backend", "db");

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            yield return Path.Combine(dir.FullName, "db");
            dir = dir.Parent;
        }
    }
}
