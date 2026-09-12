using Healingram.BuildingBlocks.Modules;
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
    public void Register(IServiceCollection services, IConfiguration configuration) { }
    public void MapEndpoints(IEndpointRouteBuilder app)
        => app.MapGet("/api/partners/ready", () => Results.Ok(new { module = Name })).WithTags("Partners");
}
