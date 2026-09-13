using System.Text.Json;
using System.Text.Json.Serialization;
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
            CancellationToken cancellationToken) =>
        {
            var items = await catalog.SearchRetreatsAsync(
                new RetreatSearchQuery(need, state, locality, duration),
                cancellationToken);
            return Results.Ok(new { items });
        });

        group.MapGet("/retreats/{slug}", async (
            string slug,
            CatalogQueryService catalog,
            CancellationToken cancellationToken) =>
        {
            var listing = await catalog.GetListingAsync(slug, cancellationToken);
            return listing is null ? Results.NotFound() : Results.Json(listing, ListingJson);
        });
    }
}
