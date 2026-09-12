using Healingram.Modules.Leads.Application;
using Healingram.Modules.Leads.Persistence;

namespace Healingram.Modules.Leads.Tests.Fakes;

internal sealed class InMemoryLeadStore : ILeadStore
{
    private readonly List<LeadEntity> _items = [];

    public IReadOnlyList<LeadEntity> All => _items.Select(Clone).ToArray();

    public Task InsertAsync(LeadEntity entity, CancellationToken cancellationToken)
    {
        _items.Add(Clone(entity));
        return Task.CompletedTask;
    }

    public Task<LeadEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_items.Where(i => i.Id == id).Select(Clone).FirstOrDefault());

    private static LeadEntity Clone(LeadEntity entity)
        => new()
        {
            Id = entity.Id,
            FullName = entity.FullName,
            PhoneE164 = entity.PhoneE164,
            Email = entity.Email,
            WhatsappConsent = entity.WhatsappConsent,
            Source = entity.Source,
            Status = entity.Status,
            ContextJson = entity.ContextJson,
            CreatedAt = entity.CreatedAt,
            History = [.. entity.History]
        };
}
