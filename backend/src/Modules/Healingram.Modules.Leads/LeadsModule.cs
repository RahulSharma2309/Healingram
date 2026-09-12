using Healingram.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Leads;

public sealed class LeadsModule : IAppModule
{
    public string Name => "Leads";
    public string Schema => "leads";
    public void Register(IServiceCollection services, IConfiguration configuration) { }
    public void MapEndpoints(IEndpointRouteBuilder app)
        => app.MapGet("/api/leads/ready", () => Results.Ok(new { module = Name })).WithTags("Leads");
}
