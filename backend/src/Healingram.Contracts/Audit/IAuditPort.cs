namespace Healingram.Contracts.Audit;

public sealed record AuditEvent(
    string Action,
    string EntityType,
    string? EntityId,
    Guid? ActorId,
    string? ActorRole,
    string? CorrelationId = null,
    object? Metadata = null);

public sealed record AuditRecord(
    Guid Id,
    string Action,
    string EntityType,
    string? EntityId,
    Guid? ActorId,
    string? ActorRole,
    string? CorrelationId,
    DateTimeOffset CreatedAt);

public interface IAuditPort
{
    Task WriteAsync(AuditEvent entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditRecord>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class NullAuditPort : IAuditPort
{
    public Task WriteAsync(AuditEvent entry, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<AuditRecord>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AuditRecord>>([]);
}
