using System.Text.Json;
using System.Text.Json.Serialization;
using Healingram.BuildingBlocks.Api;
using Healingram.BuildingBlocks.Modules;
using Healingram.Contracts.Catalog;
using Healingram.Modules.Catalog.Application;
using Healingram.Modules.Catalog.Persistence;
using Healingram.Modules.Catalog.Seed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Catalog;

public sealed class CatalogModule : IAppModule
{
    private static readonly JsonSerializerOptions ListingJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Name => "Catalog";
    public string Schema => "catalog";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICatalogStore, NpgsqlCatalogStore>();
        services.AddSingleton<CatalogQueryService>();
        services.AddSingleton<ICatalogQuotePort>(sp => sp.GetRequiredService<CatalogQueryService>());
        services.AddSingleton<ICatalogReadPort, CatalogReadPort>();
        services.AddHostedService<CatalogStartupHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog");

        group.MapGet("/ready", () => Results.Ok(new { module = Name }));

        group.MapGet("/needs", async (CatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var items = await catalog.GetNeedsAsync(cancellationToken);
            return Results.Ok(new { items });
        });

        group.MapGet("/places", async (CatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var places = await catalog.GetPlacesAsync(cancellationToken);
            return Results.Ok(places);
        });

        group.MapGet("/retreats", async (
            CatalogQueryService catalog,
            string? need,
            string? state,
            string? locality,
            string? duration,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var items = await catalog.SearchRetreatsAsync(
                new RetreatSearchQuery(need, state, locality, duration),
                cancellationToken);
            return Results.Ok(PageResult<RetreatCardDto>.Create(items, page, pageSize));
        });

        group.MapGet("/retreats/{slug}", async (
            string slug,
            CatalogQueryService catalog,
            CancellationToken cancellationToken) =>
        {
            var listing = await catalog.GetListingAsync(slug, cancellationToken);
            return listing is null ? Results.NotFound() : Results.Json(listing, ListingJson);
        });

        group.MapGet("/discovery", async (CatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var items = await catalog.GetDiscoveryAsync(cancellationToken);
            return Results.Ok(new { items });
        });

        group.MapGet("/themes", async (CatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var items = await catalog.GetThemesAsync(cancellationToken);
            return Results.Ok(new { items });
        });

        group.MapPost("/pricing/quote", async (
            PriceQuoteRequest? body,
            CatalogQueryService catalog,
            CancellationToken cancellationToken) =>
        {
            if (body is null
                || string.IsNullOrWhiteSpace(body.RetreatSlug)
                || string.IsNullOrWhiteSpace(body.ProgrammeSlug)
                || body.DurationNights is null or < 1
                || body.Guests is null or < 1)
            {
                return Results.Json(
                    new { error = "Validation failed", details = new[] { "retreat, programme, nights and guests are required" } },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var quote = await catalog.QuoteAsync(
                    new Healingram.Contracts.Catalog.CatalogQuoteRequest(
                        body.RetreatSlug.Trim(),
                        body.ProgrammeSlug.Trim(),
                        body.DurationNights.Value,
                        body.Occupancy ?? "package",
                        body.Guests.Value),
                    cancellationToken);
                return Results.Ok(new PriceQuoteDto(
                    quote.Id,
                    quote.RetreatSlug,
                    quote.ProgrammeSlug,
                    quote.DurationNights,
                    quote.Occupancy,
                    quote.Guests,
                    quote.Currency,
                    quote.BaseAmount,
                    quote.TaxAmount,
                    quote.TotalAmount,
                    quote.PriceStatus,
                    quote.PricingVersion,
                    quote.DurationNights));
            }
            catch (CatalogQuoteException ex)
            {
                return Results.Json(new { error = ex.Message, details = new[] { ex.Message } }, statusCode: StatusCodes.Status422UnprocessableEntity);
            }
        });

        var content = app.MapGroup("/api/content").WithTags("Content");
        content.MapGet("/pages", async (string? kind, CatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var items = await catalog.ListContentAsync(kind, cancellationToken);
            return Results.Ok(new { items });
        });
        content.MapGet("/pages/{slug}", async (string slug, CatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var page = await catalog.GetContentAsync(slug, cancellationToken);
            return page is null ? Results.NotFound() : Results.Ok(page);
        });
        content.MapGet("/homepage", async (CatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var sections = await catalog.GetHomepageAsync(cancellationToken);
            return Results.Ok(new { sections });
        });
        content.MapGet("/navigation", async (string? menu, CatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var key = string.IsNullOrWhiteSpace(menu) ? "customer.explore" : menu.Trim();
            var items = await catalog.GetNavigationAsync(key, cancellationToken);
            return Results.Ok(new { menu = key, items });
        });

        app.MapGet("/api/platform/settings", async (
            [Microsoft.AspNetCore.Mvc.FromServices] Microsoft.Extensions.Configuration.IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            await using var connection = new Npgsql.NpgsqlConnection(
                configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required."));
            await connection.OpenAsync(cancellationToken);
            await using var command = new Npgsql.NpgsqlCommand(
                "SELECT key, value FROM platform.settings WHERE key = ANY($1)",
                connection);
            command.Parameters.AddWithValue(new[] { "whatsapp.number" });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync(cancellationToken))
            {
                items[reader.GetString(0)] = reader.GetString(1);
            }

            return Results.Ok(new { items });
        }).WithTags("Platform");
    }
}
