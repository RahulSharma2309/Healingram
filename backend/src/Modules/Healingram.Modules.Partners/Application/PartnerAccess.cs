using Healingram.Contracts.Partners;
using Healingram.Modules.Partners.Persistence;

namespace Healingram.Modules.Partners.Application;

internal sealed class PartnerAccess(IPartnerStore store) : IPartnerAccess
{
    public Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => store.ListRetreatSlugsForUserAsync(userId, cancellationToken);
}
