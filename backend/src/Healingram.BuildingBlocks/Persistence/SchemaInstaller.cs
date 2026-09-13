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
        await EnsureMigrationsTableAsync(connection, cancellationToken);

        var applied = await LoadAppliedAsync(connection, cancellationToken);
        if (applied.Count == 0)
        {
            var legacy = await DetectLegacyAppliedAsync(connection, sqlFiles, cancellationToken);
            foreach (var id in legacy)
            {
                await RecordAppliedAsync(connection, null, id, MigrationVersion(id), cancellationToken);
                applied.Add(id);
                logger.LogInformation("Recorded already-installed schema script {Id} without re-running it", id);
            }
        }

        foreach (var sqlPath in sqlFiles)
        {
            var id = MigrationId(sqlPath);
            if (applied.Contains(id))
            {
                logger.LogInformation("Postgres schema script {Id} already applied — skipping", id);
                continue;
            }

            var sql = await File.ReadAllTextAsync(sqlPath, cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using (var command = new NpgsqlCommand(sql, connection, tx))
                {
                    command.CommandTimeout = 60;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await RecordAppliedAsync(connection, tx, id, MigrationVersion(sqlPath), cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            logger.LogInformation("Postgres schema script applied from {Path}", sqlPath);
        }
    }

    internal static string MigrationId(string path)
    {
        var normalized = path.Replace('\\', '/');
        var name = Path.GetFileName(normalized);
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }

    internal static string MigrationVersion(string path)
    {
        var name = Path.GetFileNameWithoutExtension(MigrationId(path));
        var digits = new string(name.TakeWhile(char.IsDigit).ToArray());
        return digits.Length > 0 ? digits : name;
    }

    internal static IReadOnlyList<string> PendingFiles(IReadOnlyList<string> sqlFiles, IReadOnlySet<string> applied)
        => sqlFiles.Where(file => !applied.Contains(MigrationId(file))).ToArray();

    /// <summary>
    /// Existing local databases were created by re-running numbered SQL files.
    /// When the ledger is empty but core tables already exist, record those
    /// older scripts as applied so they are not executed again.
    /// </summary>
    internal static IReadOnlyList<string> LegacyIdsToRecord(
        IReadOnlyList<string> sqlFiles,
        bool hasExistingSchema,
        bool hasFoundationTables,
        bool hasHardeningColumns)
    {
        if (!hasExistingSchema)
        {
            return [];
        }

        var recorded = new List<string>();
        foreach (var file in sqlFiles)
        {
            var id = MigrationId(file);
            if (!int.TryParse(MigrationVersion(file), out var version))
            {
                continue;
            }

            if (version < 10
                || (version == 10 && hasFoundationTables)
                || (version == 11 && hasHardeningColumns))
            {
                recorded.Add(id);
            }
        }

        return recorded;
    }

    private static async Task EnsureMigrationsTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS public.schema_migrations (
                id          text PRIMARY KEY,
                version     text NOT NULL,
                applied_at  timestamptz NOT NULL DEFAULT now()
            )
            """,
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<HashSet<string>> LoadAppliedAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT id FROM public.schema_migrations", connection);
        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(reader.GetString(0));
        }

        return applied;
    }

    private static async Task<IReadOnlyList<string>> DetectLegacyAppliedAsync(
        NpgsqlConnection connection,
        IReadOnlyList<string> sqlFiles,
        CancellationToken cancellationToken)
    {
        var hasExistingSchema = await ScalarTrueAsync(
            connection,
            "SELECT to_regclass('availability.requests') IS NOT NULL",
            cancellationToken);
        var hasFoundationTables = await ScalarTrueAsync(
            connection,
            "SELECT to_regclass('identity.admin_permissions') IS NOT NULL",
            cancellationToken);
        var hasHardeningColumns = await ScalarTrueAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'payment'
                  AND table_name = 'webhook_events'
                  AND column_name = 'processing_status')
            """,
            cancellationToken);
        return LegacyIdsToRecord(sqlFiles, hasExistingSchema, hasFoundationTables, hasHardeningColumns);
    }

    private static async Task<bool> ScalarTrueAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private static async Task RecordAppliedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string id,
        string version,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO public.schema_migrations (id, version, applied_at)
            VALUES (@id, @version, now())
            ON CONFLICT (id) DO NOTHING
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("version", version);
        await command.ExecuteNonQueryAsync(cancellationToken);
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
