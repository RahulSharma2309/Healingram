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
        var sqlPath = ResolveSqlPath(configured);
        var sql = await File.ReadAllTextAsync(sqlPath, cancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = 60;
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Postgres module schemas ensured from {Path}", sqlPath);
    }

    internal static string ResolveSqlPath(string configured)
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

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException($"Schema file not found: {configured}");
    }
}
