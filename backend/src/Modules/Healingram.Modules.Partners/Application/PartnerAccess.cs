using Healingram.Contracts.Partners;
using Healingram.Modules.Partners.Persistence;

namespace Healingram.Modules.Partners.Application;

internal sealed class PartnerAccess(IPartnerStore store) : IPartnerAccess, IPartnerAuthorization
{
    public Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => store.ListRetreatSlugsForUserAsync(userId, cancellationToken);

    public async Task<bool> CanAccessRetreatAsync(
        Guid userId,
        string retreatSlug,
        CancellationToken cancellationToken)
    {
        var slugs = await store.ListRetreatSlugsForUserAsync(userId, cancellationToken);
        return slugs.Any(slug => slug.Equals(retreatSlug, StringComparison.OrdinalIgnoreCase));
    }

    public Task<bool> CanAccessRequestAsync(Guid userId, string retreatSlug, CancellationToken cancellationToken)
        => CanAccessRetreatAsync(userId, retreatSlug, cancellationToken);

    public Task<bool> CanModifyRequestAsync(Guid userId, string retreatSlug, CancellationToken cancellationToken)
        => CanAccessRetreatAsync(userId, retreatSlug, cancellationToken);

    public Task<IReadOnlyList<PartnerMembership>> ListMembershipsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => store.ListMembershipsForUserAsync(userId, cancellationToken);
}
