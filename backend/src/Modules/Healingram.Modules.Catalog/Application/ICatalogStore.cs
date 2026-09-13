using Healingram.Modules.Catalog.Domain;
using Healingram.Modules.Catalog.Seed;

namespace Healingram.Modules.Catalog.Application;

internal interface ICatalogStore
{
    Task<IReadOnlyList<RetreatSnapshot>> ListRetreatsAsync(CancellationToken cancellationToken);
    Task<RetreatSnapshot?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task EnsurePublicSchemaAsync(CancellationToken cancellationToken);
    Task<int> CountRetreatsAsync(CancellationToken cancellationToken);
    Task SeedAsync(IReadOnlyList<LaunchRetreatSeed> retreats, CancellationToken cancellationToken);
}
