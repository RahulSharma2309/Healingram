namespace Healingram.Contracts.Partners;

/// <summary>
/// Retreat slugs a partner user may see. Availability must not query partners.* tables.
/// </summary>
public sealed record PartnerMembership(
    Guid PartnerId,
    string PartnerName,
    string MembershipRole,
    string Status);

public interface IPartnerAccess
{
    Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> CanAccessRetreatAsync(Guid userId, string retreatSlug, CancellationToken cancellationToken);

    Task<IReadOnlyList<PartnerMembership>> ListMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> CanAccessPartnerAsync(Guid userId, Guid partnerId, CancellationToken cancellationToken);
}
