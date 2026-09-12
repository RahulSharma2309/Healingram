using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Domain;
using Healingram.Modules.Catalog.Seed;

namespace Healingram.Modules.Catalog.Tests.Fakes;

internal sealed class InMemoryCatalogStore : ICatalogStore
{
    public List<RetreatSnapshot> Retreats { get; } = [];

    public Task<IReadOnlyList<RetreatSnapshot>> ListRetreatsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<RetreatSnapshot>>(Retreats.ToArray());

    public Task<RetreatSnapshot?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(Retreats.FirstOrDefault(r => r.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)));

    public Task EnsurePublicSchemaAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<int> CountRetreatsAsync(CancellationToken cancellationToken)
        => Task.FromResult(Retreats.Count);

    public Task SeedAsync(IReadOnlyList<LaunchRetreatSeed> retreats, CancellationToken cancellationToken)
    {
        var existing = Retreats.Select(r => r.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var retreat in retreats)
        {
            if (existing.Contains(retreat.Slug))
            {
                continue;
            }

            Retreats.Add(LaunchCatalogMapper.ToSnapshot(retreat));
            existing.Add(retreat.Slug);
        }

        return Task.CompletedTask;
    }
}
