using Healingram.Modules.Availability.Application;
using Healingram.Modules.Availability.Persistence;

namespace Healingram.Modules.Availability.Tests.Fakes;

internal sealed class InMemoryAvailabilityStore : IAvailabilityStore
{
    private readonly List<AvailabilityRequestEntity> _items = [];
    private long _sequence = 10000;

    public IReadOnlyList<AvailabilityRequestEntity> Items => _items;

    public Task<AvailabilityRequestEntity?> FindByIdempotencyKeyAsync(string key, CancellationToken cancellationToken)
        => Task.FromResult(_items.FirstOrDefault(i => i.IdempotencyKey == key));

    public Task<AvailabilityRequestEntity?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken)
        => Task.FromResult(_items.FirstOrDefault(i => i.PublicId == publicId));

    public Task<long> NextPublicSequenceAsync(CancellationToken cancellationToken)
        => Task.FromResult(Interlocked.Increment(ref _sequence));

    public Task InsertAsync(AvailabilityRequestEntity entity, CancellationToken cancellationToken)
    {
        if (_items.Any(i => i.IdempotencyKey == entity.IdempotencyKey))
        {
            throw new DuplicateIdempotencyException();
        }

        _items.Add(Clone(entity));
        return Task.CompletedTask;
    }

    public Task SavePartnerResponseAsync(AvailabilityRequestEntity entity, StatusHistoryEntry history, CancellationToken cancellationToken)
    {
        var stored = _items.First(i => i.Id == entity.Id);
        if (!string.Equals(stored.SnapshotJson, entity.SnapshotJson, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Price snapshot is immutable");
        }

        stored.Status = entity.Status;
        stored.PartnerViewedAt = entity.PartnerViewedAt;
        stored.PartnerRespondedAt = entity.PartnerRespondedAt;
        stored.FinalAmountInr = entity.FinalAmountInr;
        stored.AlternativeJson = entity.AlternativeJson;
        stored.History.Add(history);
        return Task.CompletedTask;
    }

    public Task AddAdminNoteAsync(Guid requestId, AdminNoteEntry note, CancellationToken cancellationToken)
    {
        _items.First(i => i.Id == requestId).InternalNotes.Add(note);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AvailabilityRequestEntity>> ListByStatusesAsync(
        IReadOnlyList<string> statuses,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AvailabilityRequestEntity>>(
            _items.Where(i => statuses.Contains(i.Status)).OrderBy(i => i.RequestedAt).Select(Clone).ToArray());

    public Task<IReadOnlyList<AvailabilityRequestEntity>> ListByCustomerUserIdAsync(
        Guid customerUserId,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AvailabilityRequestEntity>>(
            _items
                .Where(i => i.CustomerUserId == customerUserId)
                .OrderByDescending(i => i.RequestedAt)
                .Select(Clone)
                .ToArray());

    public Task MarkPartnerViewedAsync(IReadOnlyList<Guid> ids, DateTimeOffset viewedAt, CancellationToken cancellationToken)
    {
        foreach (var item in _items.Where(i => ids.Contains(i.Id) && i.PartnerViewedAt is null))
        {
            item.PartnerViewedAt = viewedAt;
        }

        return Task.CompletedTask;
    }

    private static AvailabilityRequestEntity Clone(AvailabilityRequestEntity entity)
        => new()
        {
            Id = entity.Id,
            PublicId = entity.PublicId,
            CustomerUserId = entity.CustomerUserId,
            CustomerName = entity.CustomerName,
            CustomerEmail = entity.CustomerEmail,
            CustomerPhone = entity.CustomerPhone,
            RetreatId = entity.RetreatId,
            ProgrammeId = entity.ProgrammeId,
            RetreatSlug = entity.RetreatSlug,
            ProgrammeSlug = entity.ProgrammeSlug,
            Status = entity.Status,
            SnapshotJson = entity.SnapshotJson,
            IdempotencyKey = entity.IdempotencyKey,
            RequestedAt = entity.RequestedAt,
            PartnerViewedAt = entity.PartnerViewedAt,
            PartnerRespondedAt = entity.PartnerRespondedAt,
            FinalAmountInr = entity.FinalAmountInr,
            AlternativeJson = entity.AlternativeJson,
            History = [.. entity.History],
            InternalNotes = [.. entity.InternalNotes]
        };
}
