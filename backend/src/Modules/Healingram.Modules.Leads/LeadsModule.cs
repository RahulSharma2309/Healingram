using System.Runtime.CompilerServices;
using Healingram.BuildingBlocks.Modules;
using Healingram.Modules.Leads.Application;
using Healingram.Modules.Leads.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Healingram.Modules.Leads.Tests")]

namespace Healingram.Modules.Leads;

public sealed class LeadsModule : IAppModule
{
    public string Name => "Leads";
    public string Schema => "leads";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ILeadStore, PostgresLeadStore>();
        services.AddScoped<LeadService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/leads/ready", () => Results.Ok(new { module = Name })).WithTags("Leads");
        LeadsEndpoints.Map(app);
    }
}
