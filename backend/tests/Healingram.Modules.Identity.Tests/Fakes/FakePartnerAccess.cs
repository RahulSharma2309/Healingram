using Healingram.Contracts.Partners;

namespace Healingram.Modules.Identity.Tests.Fakes;

internal sealed class FakePartnerAccess : IPartnerAccess
{
    public Dictionary<Guid, List<PartnerMembership>> Memberships { get; } = [];
    public Dictionary<Guid, List<string>> RetreatSlugs { get; } = [];

    public Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(
            RetreatSlugs.TryGetValue(userId, out var slugs) ? slugs : []);

    public Task<bool> CanAccessRetreatAsync(Guid userId, string retreatSlug, CancellationToken cancellationToken)
        => Task.FromResult(
            RetreatSlugs.TryGetValue(userId, out var slugs)
            && slugs.Any(slug => slug.Equals(retreatSlug, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<PartnerMembership>> ListMembershipsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PartnerMembership>>(
            Memberships.TryGetValue(userId, out var items) ? items : []);

    public Task<bool> CanAccessPartnerAsync(Guid userId, Guid partnerId, CancellationToken cancellationToken)
        => Task.FromResult(
            Memberships.TryGetValue(userId, out var items)
            && items.Any(item =>
                item.PartnerId == partnerId
                && string.Equals(item.Status, "active", StringComparison.OrdinalIgnoreCase)));

    public void Grant(Guid userId, PartnerMembership membership, params string[] retreatSlugs)
    {
        if (!Memberships.TryGetValue(userId, out var items))
        {
            items = [];
            Memberships[userId] = items;
        }

        items.Add(membership);
        RetreatSlugs[userId] = retreatSlugs.ToList();
    }
}
