using System.Security.Claims;
using Healingram.Contracts.Identity;
using Healingram.Contracts.Partners;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Healingram.Modules.Partners;

internal static class PartnerEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/partner/me", async (
            ClaimsPrincipal user,
            IPartnerAccess partners,
            CancellationToken cancellationToken) =>
        {
            if (!TryUserId(user, out var userId))
            {
                return Results.Json(new { error = "Unauthorized" }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var memberships = await partners.ListMembershipsForUserAsync(userId, cancellationToken);
            var slugs = await partners.ListRetreatSlugsForUserAsync(userId, cancellationToken);
            return Results.Ok(new
            {
                userId,
                memberships = memberships.Select(m => new
                {
                    partnerId = m.PartnerId,
                    partnerName = m.PartnerName,
                    role = m.MembershipRole,
                    status = m.Status
                }),
                retreatSlugs = slugs
            });
        }).RequireAuthorization(IdentityPolicies.PartnerWrite).WithTags("Partners");

        app.MapGet("/api/partner/retreats", async (
            ClaimsPrincipal user,
            IPartnerAccess partners,
            CancellationToken cancellationToken) =>
        {
            if (!TryUserId(user, out var userId))
            {
                return Results.Json(new { error = "Unauthorized" }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var slugs = await partners.ListRetreatSlugsForUserAsync(userId, cancellationToken);
            return Results.Ok(new { items = slugs.Select(slug => new { slug }) });
        }).RequireAuthorization(IdentityPolicies.PartnerWrite).WithTags("Partners");
    }

    private static bool TryUserId(ClaimsPrincipal user, out Guid userId)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out userId);
    }
}
