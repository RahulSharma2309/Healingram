using Healingram.BuildingBlocks.Modules;
using Healingram.Contracts.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Catalog;

public sealed class CatalogModule : IAppModule
{
    public string Name => "Catalog";
    public string Schema => "catalog";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICatalogReadPort, EmptyCatalogReadPort>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog");
        group.MapGet("/needs", () => Results.Ok(new
        {
            items = new[]
            {
                new { slug = "stress-burnout", label = "Stress and Burnout" },
                new { slug = "ayurveda", label = "Ayurveda" },
                new { slug = "panchakarma", label = "Panchakarma" },
                new { slug = "yoga", label = "Yoga" },
                new { slug = "meditation", label = "Meditation" },
                new { slug = "rejuvenation", label = "Rejuvenation" },
                new { slug = "weekend-wellness", label = "Weekend Wellness" },
                new { slug = "weight-metabolic", label = "Weight and Metabolic Wellness" }
            }
        }));
    }
}

internal sealed class EmptyCatalogReadPort : ICatalogReadPort
{
    public Task<IReadOnlyList<string>> GetPublicRetreatSlugsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([]);
}
