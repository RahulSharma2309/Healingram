namespace Healingram.Contracts.Catalog;

public interface ICatalogReadPort
{
    Task<IReadOnlyList<string>> GetPublicRetreatSlugsAsync(CancellationToken cancellationToken);
}
