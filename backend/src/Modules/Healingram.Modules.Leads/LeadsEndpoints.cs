using System.Text.Json;
using Healingram.Contracts.Identity;
using Healingram.Modules.Leads.Application;
using Healingram.Modules.Leads.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Healingram.Modules.Leads;

internal static class LeadsEndpoints
{
    private static readonly JsonSerializerOptions Json = LeadJson.Options;

    public static void Map(IEndpointRouteBuilder app)
    {
        var leads = app.MapGroup("/api/leads").WithTags("Leads");

        leads.MapPost("/", (
            CreateLeadRequest? body,
            LeadService service,
            CancellationToken cancellationToken)
            => Handle(service.CreateAsync(body, cancellationToken)));

        leads.MapGet("/{id:guid}", (
            Guid id,
            LeadService service,
            CancellationToken cancellationToken)
            => Handle(service.GetAsync(id, cancellationToken)))
            .RequireAuthorization(IdentityPolicies.AdminWrite);
    }

    internal static async Task<IResult> Handle(Task<LeadOutcome> action)
    {
        var result = await action;
        return result.Kind switch
        {
            LeadOutcomeKind.Created when result.Entity is not null
                => Results.Json(ToCreateDto(result.Entity), Json, statusCode: StatusCodes.Status201Created),
            LeadOutcomeKind.Ok when result.Entity is not null
                => Results.Json(ToLeadDto(result.Entity), Json),
            LeadOutcomeKind.Validation
                => Results.Json(new { error = result.Error, details = result.Details ?? [] }, Json, statusCode: StatusCodes.Status400BadRequest),
            LeadOutcomeKind.NotFound
                => Results.Json(new { error = result.Error, details = Array.Empty<string>() }, Json, statusCode: StatusCodes.Status404NotFound),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    internal static object ToCreateDto(LeadEntity entity)
        => new { id = entity.Id.ToString(), status = entity.Status };

    internal static object ToLeadDto(LeadEntity entity)
    {
        using var context = JsonDocument.Parse(string.IsNullOrWhiteSpace(entity.ContextJson) ? "{}" : entity.ContextJson);
        var root = context.RootElement.Clone();

        return new Dictionary<string, object?>
        {
            ["id"] = entity.Id.ToString(),
            ["status"] = entity.Status,
            ["fullName"] = entity.FullName,
            ["phone"] = entity.PhoneE164,
            ["email"] = entity.Email,
            ["whatsappConsent"] = entity.WhatsappConsent,
            ["source"] = entity.Source,
            ["helpType"] = ReadString(root, "helpType"),
            ["need"] = ReadString(root, "need"),
            ["travelWindow"] = ReadString(root, "travelWindow"),
            ["context"] = root,
            ["createdAt"] = entity.CreatedAt,
            ["history"] = entity.History.Select(h => new
            {
                fromStatus = h.FromStatus,
                toStatus = h.ToStatus,
                actorId = h.ActorId,
                occurredAt = h.OccurredAt
            }).ToArray()
        };
    }

    private static string? ReadString(JsonElement root, string name)
        => root.ValueKind == JsonValueKind.Object
           && root.TryGetProperty(name, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
