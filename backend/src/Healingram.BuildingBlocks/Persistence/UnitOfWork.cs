using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Healingram.BuildingBlocks.Persistence;

public interface IUnitOfWork
{
    Task<IUnitOfWorkScope> BeginAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkScope : IAsyncDisposable
{
    NpgsqlConnection Connection { get; }
    NpgsqlTransaction Transaction { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public static class AmbientUnitOfWork
{
    private static readonly AsyncLocal<IUnitOfWorkScope?> CurrentScope = new();

    public static IUnitOfWorkScope? Current => CurrentScope.Value;

    internal static void Set(IUnitOfWorkScope? scope) => CurrentScope.Value = scope;
}

public sealed class PostgresUnitOfWork(IConfiguration configuration) : IUnitOfWork
{
    public async Task<IUnitOfWorkScope> BeginAsync(CancellationToken cancellationToken = default)
    {
        if (AmbientUnitOfWork.Current is { } existing)
        {
            return new NestedUnitOfWorkScope(existing);
        }

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var scope = new RootUnitOfWorkScope(connection, transaction);
        AmbientUnitOfWork.Set(scope);
        return scope;
    }
}

file sealed class NestedUnitOfWorkScope(IUnitOfWorkScope inner) : IUnitOfWorkScope
{
    public NpgsqlConnection Connection => inner.Connection;
    public NpgsqlTransaction Transaction => inner.Transaction;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

file sealed class RootUnitOfWorkScope(NpgsqlConnection connection, NpgsqlTransaction transaction) : IUnitOfWorkScope
{
    private bool _committed;

    public NpgsqlConnection Connection { get; } = connection;
    public NpgsqlTransaction Transaction { get; } = transaction;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await Transaction.CommitAsync(cancellationToken);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_committed)
            {
                await Transaction.RollbackAsync();
            }
        }
        finally
        {
            await Transaction.DisposeAsync();
            await Connection.DisposeAsync();
            AmbientUnitOfWork.Set(null);
        }
    }
}

public static class PostgresWork
{
    public static async Task<T> WriteAsync<T>(
        string connectionString,
        Func<NpgsqlConnection, NpgsqlTransaction?, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        bool beginLocalTransaction = false)
    {
        if (AmbientUnitOfWork.Current is { } ambient)
        {
            return await action(ambient.Connection, ambient.Transaction, cancellationToken);
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        if (!beginLocalTransaction)
        {
            return await action(connection, null, cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static string ConnectionString(IConfiguration configuration)
        => configuration.GetConnectionString("Postgres")
           ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
}
