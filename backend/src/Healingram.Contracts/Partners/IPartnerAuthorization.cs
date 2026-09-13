namespace Healingram.Contracts.Partners;

public interface IPartnerAuthorization
{
    Task<bool> CanAccessRequestAsync(Guid userId, string retreatSlug, CancellationToken cancellationToken);

    Task<bool> CanModifyRequestAsync(Guid userId, string retreatSlug, CancellationToken cancellationToken);
}
