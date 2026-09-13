namespace Healingram.Modules.Partners.Persistence;

internal interface IPartnerStore
{
    Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task SeedLocalPartnerAsync(CancellationToken cancellationToken);
}
