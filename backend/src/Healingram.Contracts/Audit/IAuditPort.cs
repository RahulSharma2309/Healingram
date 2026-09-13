namespace Healingram.Contracts.Audit;

public sealed record AuditEvent(
    string Action,
    string EntityType,
    string? EntityId,
    Guid? ActorId,
    string? ActorRole,
    string? CorrelationId = null,
    object? Metadata = null);

public interface IAuditPort
{
    Task WriteAsync(AuditEvent entry, CancellationToken cancellationToken);
}

public sealed class NullAuditPort : IAuditPort
{
    public Task WriteAsync(AuditEvent entry, CancellationToken cancellationToken) => Task.CompletedTask;
}
