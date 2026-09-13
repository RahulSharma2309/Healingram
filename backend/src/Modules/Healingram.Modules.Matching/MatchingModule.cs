using System.Text.Json;
using Healingram.BuildingBlocks.Modules;
using Healingram.Modules.Matching.Application;
using Healingram.Modules.Matching.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Matching;

public sealed class MatchingModule : IAppModule
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string Name => "Matching";
    public string Schema => "matching";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMatchSessionStore, PostgresMatchSessionStore>();
        services.AddScoped<IMatchOptionStore, PostgresMatchOptionStore>();
        services.AddScoped<MatchingService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matching").WithTags("Matching");
        group.MapGet("/ready", () => Results.Ok(new { module = Name }));
        group.MapGet("/options", async (MatchingService matching, CancellationToken cancellationToken) =>
        {
            var set = await matching.GetOptionsAsync(cancellationToken);
            return Results.Ok(new
            {
                questions = set.Questions.Select(q => new
                {
                    key = q.Key,
                    label = q.Label,
                    selectionMode = q.SelectionMode,
                    sortOrder = q.SortOrder,
                    options = q.Options.Select(o => new
                    {
                        key = o.Key,
                        label = o.Label,
                        description = o.Description,
                        iconKey = o.IconKey,
                        sortOrder = o.SortOrder,
                        themeSlugs = o.ThemeSlugs
                    })
                })
            });
        });
        group.MapPost("/sessions", async (
            CreateMatchSessionRequest? body,
            MatchingService matching,
            CancellationToken cancellationToken) =>
        {
            var result = await matching.CreateSessionAsync(body, cancellationToken);
            return result.Status switch
            {
                MatchingStatus.Ok => Results.Ok(new MatchSessionResponse(result.Id!.Value, result.Matches!)),
                _ => Results.Json(
                    new { error = result.Error ?? "Validation failed", details = result.Details ?? [] },
                    Json,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        });
    }
}
