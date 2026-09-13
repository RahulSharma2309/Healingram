using System.Security.Claims;
using System.Text.Json;
using Healingram.Contracts.Identity;
using Healingram.Modules.Availability.Application;
using Healingram.Modules.Availability.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Healingram.Modules.Availability;

internal static class AvailabilityEndpoints
{
    private static readonly JsonSerializerOptions Json = AvailabilityJson.Options;

    public static void Map(IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/api/availability/requests").WithTags("Availability");

        requests.MapPost("/", (
            CreateAvailabilityRequest body,
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
            => Handle(service.CreateAsync(body, ActorOf(user), cancellationToken), includeNotes: false))
            .RequireRateLimiting("sensitive");

        requests.MapGet("/{publicId}", (
            string publicId,
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
            => Handle(
                service.GetAsync(publicId, ActorOf(user), includeInternalNotes: false, cancellationToken),
                includeNotes: false));

        requests.MapPost("/{publicId}/confirm", (
            string publicId,
            ConfirmAvailabilityRequest? body,
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
            => Handle(
                service.ConfirmAsync(publicId, body ?? new ConfirmAvailabilityRequest(null), ActorOf(user), cancellationToken),
                includeNotes: false))
            .RequireAuthorization(IdentityPolicies.PartnerWrite);

        requests.MapPost("/{publicId}/alternative", (
            string publicId,
            AlternativeAvailabilityRequest body,
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
            => Handle(service.OfferAlternativeAsync(publicId, body, ActorOf(user), cancellationToken), includeNotes: false))
            .RequireAuthorization(IdentityPolicies.PartnerWrite);

        requests.MapPost("/{publicId}/unavailable", (
            string publicId,
            UnavailableAvailabilityRequest body,
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
            => Handle(service.MarkUnavailableAsync(publicId, body, ActorOf(user), cancellationToken), includeNotes: false))
            .RequireAuthorization(IdentityPolicies.PartnerWrite);

        requests.MapPost("/{publicId}/accept-alternative", (
            string publicId,
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
            => Handle(service.AcceptAlternativeAsync(publicId, ActorOf(user), cancellationToken), includeNotes: false))
            .RequireAuthorization();

        app.MapGet("/api/availability/mine", async (
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var actor = ActorOf(user);
            if (actor.UserId is null)
            {
                return Results.Json(
                    new { error = "Verify it's you to view your requests", details = Array.Empty<string>() },
                    Json,
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var items = await service.ListMineAsync(actor, cancellationToken);
            return Results.Json(new { items = items.Select(e => ToDto(e, includeNotes: false)) }, Json);
        }).RequireAuthorization().WithTags("Availability");

        app.MapGet("/api/guest/requests/{publicId}", (
            string publicId,
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
            => Handle(
                service.GetAsync(publicId, ActorOf(user), includeInternalNotes: false, cancellationToken),
                includeNotes: false))
            .RequireAuthorization()
            .WithTags("Availability");

        app.MapGet("/api/trips", async (
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var actor = ActorOf(user);
            if (actor.UserId is null)
            {
                return Results.Json(TripGroupsDto.Empty, Json);
            }

            var groups = await service.ListTripsAsync(actor, cancellationToken);
            return Results.Json(groups, Json);
        }).RequireAuthorization().WithTags("Account");

        app.MapGet("/api/partner/availability", async (
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var items = await service.ListPartnerPendingAsync(ActorOf(user), cancellationToken);
            return Results.Json(new { items = items.Select(e => ToDto(e, includeNotes: false)) }, Json);
        }).RequireAuthorization(IdentityPolicies.PartnerWrite).WithTags("Partners");

        app.MapGet("/api/admin/availability", async (
            AvailabilityService service,
            CancellationToken cancellationToken) =>
        {
            var items = await service.ListAdminPendingAsync(cancellationToken);
            return Results.Json(new { items = items.Select(e => ToDto(e, includeNotes: true)) }, Json);
        }).RequireAuthorization(IdentityPolicies.AdminRequestsRead).WithTags("Admin");

        app.MapPost("/api/admin/availability/{publicId}/note", (
            string publicId,
            AdminNoteRequest body,
            AvailabilityService service,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
            => Handle(service.AddAdminNoteAsync(publicId, body, ActorOf(user), cancellationToken), includeNotes: true))
            .RequireAuthorization(IdentityPolicies.AdminRequestsManage)
            .WithTags("Admin");
    }

    private static async Task<IResult> Handle(Task<AvailabilityOutcome> action, bool includeNotes)
    {
        var result = await action;
        return result.Kind switch
        {
            AvailabilityOutcomeKind.Created when result.Entity is not null
                => Results.Json(ToDto(result.Entity, includeNotes), Json, statusCode: StatusCodes.Status201Created),
            AvailabilityOutcomeKind.Ok or AvailabilityOutcomeKind.Replayed when result.Entity is not null
                => Results.Json(ToDto(result.Entity, includeNotes), Json),
            AvailabilityOutcomeKind.Conflict when result.Entity is not null
                => Results.Json(ToDto(result.Entity, includeNotes), Json, statusCode: StatusCodes.Status409Conflict),
            AvailabilityOutcomeKind.Conflict
                => Results.Json(new { error = result.Error ?? "Conflict", details = result.Details ?? [] }, Json, statusCode: StatusCodes.Status409Conflict),
            AvailabilityOutcomeKind.Validation
                => Results.Json(new { error = result.Error, details = result.Details ?? [] }, Json, statusCode: StatusCodes.Status400BadRequest),
            AvailabilityOutcomeKind.NotFound
                => Results.Json(new { error = result.Error, details = Array.Empty<string>() }, Json, statusCode: StatusCodes.Status404NotFound),
            AvailabilityOutcomeKind.IllegalTransition
                => Results.Json(new { error = result.Error, details = result.Details ?? [] }, Json, statusCode: StatusCodes.Status409Conflict),
            AvailabilityOutcomeKind.Forbidden
                => Results.Json(new { error = result.Error, details = result.Details ?? [] }, Json, statusCode: StatusCodes.Status403Forbidden),
            AvailabilityOutcomeKind.Unauthorized
                => Results.Json(new { error = result.Error, details = result.Details ?? [] }, Json, statusCode: StatusCodes.Status401Unauthorized),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    internal static object ToDto(AvailabilityRequestEntity entity, bool includeNotes)
    {
        using var snapshot = JsonDocument.Parse(entity.SnapshotJson);
        object? alternative = null;
        if (!string.IsNullOrWhiteSpace(entity.AlternativeJson))
        {
            using var alt = JsonDocument.Parse(entity.AlternativeJson);
            alternative = alt.RootElement.Clone();
        }

        var dto = new Dictionary<string, object?>
        {
            ["publicId"] = entity.PublicId,
            ["status"] = entity.Status,
            ["snapshot"] = snapshot.RootElement.Clone(),
            ["customerName"] = entity.CustomerName,
            ["email"] = entity.CustomerEmail,
            ["phone"] = entity.CustomerPhone,
            ["retreatSlug"] = entity.RetreatSlug,
            ["programmeSlug"] = entity.ProgrammeSlug,
            ["requestedAt"] = entity.RequestedAt,
            ["partnerViewedAt"] = entity.PartnerViewedAt,
            ["partnerRespondedAt"] = entity.PartnerRespondedAt,
            ["finalAmountInr"] = entity.FinalAmountInr,
            ["alternative"] = alternative,
            ["history"] = entity.History.Select(h => new
            {
                fromStatus = h.FromStatus,
                toStatus = h.ToStatus,
                actorRole = h.ActorRole,
                occurredAt = h.OccurredAt,
                reason = h.Reason
            }).ToArray()
        };

        if (includeNotes)
        {
            dto["internalNotes"] = entity.InternalNotes.Select(n => new
            {
                body = n.Body,
                createdAt = n.CreatedAt
            }).ToArray();
        }

        return dto;
    }

    internal static Actor ActorOf(ClaimsPrincipal user)
    {
        var authenticated = user.Identity?.IsAuthenticated == true;
        var roles = authenticated ? RoleAuthorization.GetRoles(user) : [];
        var role = authenticated
            ? RoleAuthorization.PrimaryRole(roles)
            : Roles.Customer;
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        Guid? userId = Guid.TryParse(raw, out var parsed) ? parsed : null;
        return new Actor(
            role,
            userId,
            RoleAuthorization.GetPurpose(user),
            RoleAuthorization.GetScopedRequestId(user),
            roles,
            RoleAuthorization.GetAuthKind(user));
    }
}
