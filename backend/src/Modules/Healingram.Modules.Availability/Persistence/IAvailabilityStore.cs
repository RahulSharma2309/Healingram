using Healingram.Modules.Availability.Application;

namespace Healingram.Modules.Availability.Persistence;

internal interface IAvailabilityStore
{
    Task<AvailabilityRequestEntity?> FindByIdempotencyKeyAsync(string key, CancellationToken cancellationToken);
    Task<AvailabilityRequestEntity?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken);
    Task<long> NextPublicSequenceAsync(CancellationToken cancellationToken);
    Task InsertAsync(AvailabilityRequestEntity entity, CancellationToken cancellationToken);
    Task<bool> TrySavePartnerResponseAsync(
        AvailabilityRequestEntity entity,
        StatusHistoryEntry history,
        string expectedFromStatus,
        CancellationToken cancellationToken);
    Task AddAdminNoteAsync(Guid requestId, AdminNoteEntry note, CancellationToken cancellationToken);
    Task<IReadOnlyList<AvailabilityRequestEntity>> ListByStatusesAsync(
        IReadOnlyList<string> statuses,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? retreatSlugs = null);
    Task<IReadOnlyList<AvailabilityRequestEntity>> ListByCustomerUserIdAsync(Guid customerUserId, CancellationToken cancellationToken);
    Task MarkPartnerViewedAsync(IReadOnlyList<Guid> ids, DateTimeOffset viewedAt, CancellationToken cancellationToken);
}
