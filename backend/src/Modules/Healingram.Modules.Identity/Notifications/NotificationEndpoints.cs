using System.Security.Claims;
using Healingram.BuildingBlocks.Notifications;
using Healingram.Modules.Identity.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Healingram.Modules.Identity.Notifications;

internal static class NotificationEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal principal,
            IUserInboxPort inbox,
            CancellationToken cancellationToken) =>
        {
            if (!AuthEndpoints.TryGetUserId(principal, out var userId))
            {
                return AuthEndpoints.AuthHttp.Unauthorized("Unauthorized");
            }

            var items = await inbox.ListForUserAsync(userId, cancellationToken);
            return Results.Ok(new
            {
                items = items.Select(i => new
                {
                    id = i.Id,
                    kind = i.Kind,
                    title = i.Title,
                    body = i.Body,
                    entityType = i.EntityType,
                    entityId = i.EntityId,
                    createdAt = i.CreatedAt,
                    readAt = i.ReadAt,
                    read = i.ReadAt is not null
                })
            });
        });

        group.MapPost("/{id:guid}/read", async (
            Guid id,
            ClaimsPrincipal principal,
            IUserInboxPort inbox,
            CancellationToken cancellationToken) =>
        {
            if (!AuthEndpoints.TryGetUserId(principal, out var userId))
            {
                return AuthEndpoints.AuthHttp.Unauthorized("Unauthorized");
            }

            await inbox.MarkReadAsync(userId, id, cancellationToken);
            return Results.NoContent();
        });
    }
}
