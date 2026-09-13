namespace Healingram.BuildingBlocks.Notifications;

public sealed record UserInboxItem(
    Guid Id,
    Guid UserId,
    string Kind,
    string Title,
    string Body,
    string? EntityType,
    string? EntityId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public interface IUserInboxPort
{
    Task WriteAsync(
        Guid userId,
        string kind,
        string title,
        string body,
        string? entityType,
        string? entityId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserInboxItem>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20);

    Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}

public sealed class NullUserInboxPort : IUserInboxPort
{
    public Task WriteAsync(
        Guid userId,
        string kind,
        string title,
        string body,
        string? entityType,
        string? entityId,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<IReadOnlyList<UserInboxItem>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
        => Task.FromResult<IReadOnlyList<UserInboxItem>>([]);

    public Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        => Task.FromResult(false);
}
