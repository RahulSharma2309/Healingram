using Healingram.BuildingBlocks.Modules;
using Healingram.Contracts.Partners;
using Healingram.Modules.Partners.Application;
using Healingram.Modules.Partners.Persistence;
using Healingram.Modules.Partners.Seed;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Partners;

public sealed class PartnersModule : IAppModule
{
    public string Name => "Partners";
    public string Schema => "partners";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPartnerStore, PostgresPartnerStore>();
        services.AddScoped<PartnerAccess>();
        services.AddScoped<IPartnerAccess>(sp => sp.GetRequiredService<PartnerAccess>());
        services.AddScoped<IPartnerAuthorization>(sp => sp.GetRequiredService<PartnerAccess>());
        services.AddHostedService<PartnerSeedHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
        => app.MapGet("/api/partners/ready", () => Results.Ok(new { module = Name })).WithTags("Partners");
}
