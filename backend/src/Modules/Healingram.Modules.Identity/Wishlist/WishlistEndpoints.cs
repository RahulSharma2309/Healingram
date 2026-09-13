using System.Security.Claims;
using System.Text.Json;
using Healingram.Modules.Identity.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Healingram.Modules.Identity.Wishlist;

internal static class WishlistEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Map(IEndpointRouteBuilder app)
    {
        var wishlist = app.MapGroup("/api/wishlist").WithTags("Wishlist").RequireAuthorization();

        wishlist.MapGet("/", async (ClaimsPrincipal principal, WishlistService service, CancellationToken ct) =>
        {
            if (!AuthEndpoints.TryGetUserId(principal, out var userId))
            {
                return WishlistHttp.Unauthorized();
            }

            return WishlistHttp.From(await service.ListAsync(userId, ct));
        });

        wishlist.MapPost("/", async (AddWishlistRequest? body, ClaimsPrincipal principal, WishlistService service, CancellationToken ct) =>
        {
            if (!AuthEndpoints.TryGetUserId(principal, out var userId))
            {
                return WishlistHttp.Unauthorized();
            }

            return WishlistHttp.From(await service.AddAsync(userId, body, ct));
        });

        wishlist.MapDelete("/{slug}", async (string slug, ClaimsPrincipal principal, WishlistService service, CancellationToken ct) =>
        {
            if (!AuthEndpoints.TryGetUserId(principal, out var userId))
            {
                return WishlistHttp.Unauthorized();
            }

            return WishlistHttp.From(await service.RemoveAsync(userId, slug, ct));
        });
    }

    internal static class WishlistHttp
    {
        public static IResult From(WishlistResult result) => result.Status switch
        {
            WishlistStatus.Ok when result.List is not null => Results.Ok(result.List),
            WishlistStatus.Created when result.List is not null => Results.Created("/api/wishlist", result.List),
            WishlistStatus.NoContent => Results.NoContent(),
            WishlistStatus.Validation => Results.Json(
                new { error = result.Error, details = result.Details ?? [] },
                Json,
                statusCode: StatusCodes.Status400BadRequest),
            WishlistStatus.Unauthorized => Unauthorized(),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };

        public static IResult Unauthorized()
            => Results.Json(
                new { error = "Unauthorized", details = Array.Empty<string>() },
                Json,
                statusCode: StatusCodes.Status401Unauthorized);
    }
}
