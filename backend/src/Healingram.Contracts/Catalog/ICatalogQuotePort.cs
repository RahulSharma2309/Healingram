namespace Healingram.Contracts.Catalog;

public sealed class CatalogQuoteException(string message) : Exception(message);

public sealed record CatalogQuoteRequest(
    string RetreatSlug,
    string ProgrammeSlug,
    int DurationNights,
    string Occupancy,
    int Guests);

public sealed record CatalogQuote(
    Guid Id,
    string RetreatSlug,
    string ProgrammeSlug,
    int DurationNights,
    string Occupancy,
    int Guests,
    string Currency,
    decimal? BaseAmount,
    decimal? TaxAmount,
    decimal? TotalAmount,
    string PriceStatus,
    string PricingVersion,
    string SnapshotJson);

public interface ICatalogQuotePort
{
    Task<CatalogQuote> QuoteAsync(CatalogQuoteRequest request, CancellationToken cancellationToken);

    Task<CatalogQuote?> GetQuoteAsync(Guid id, CancellationToken cancellationToken);
}
