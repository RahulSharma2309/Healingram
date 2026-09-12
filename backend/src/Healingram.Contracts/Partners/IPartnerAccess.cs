namespace Healingram.Contracts.Partners;

/// <summary>
/// Retreat slugs a partner user may see. Availability must not query partners.* tables.
/// </summary>
public interface IPartnerAccess
{
    Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(Guid userId, CancellationToken cancellationToken);
}
