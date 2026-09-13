using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Healingram.Modules.Identity.Auth;

internal static class AuthEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Map(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");
        auth.MapPost("/register", (RegisterRequest body, AuthService service, CancellationToken ct)
            => Handle(() => service.RegisterAsync(body, ct)))
            .RequireRateLimiting("sensitive");
        auth.MapPost("/login", (LoginRequest body, AuthService service, CancellationToken ct)
            => Handle(() => service.LoginAsync(body, ct)))
            .RequireRateLimiting("sensitive");
        auth.MapPost("/refresh", (RefreshRequest body, AuthService service, CancellationToken ct)
            => Handle(() => service.RefreshAsync(body, ct)))
            .RequireRateLimiting("sensitive");
        auth.MapPost("/logout", (LogoutRequest? body, AuthService service, CancellationToken ct)
            => Handle(() => service.LogoutAsync(body ?? new LogoutRequest(null), ct)));
        auth.MapPost("/guest/verify-start", (GuestVerifyStartRequest body, AuthService service, CancellationToken ct)
            => Handle(() => service.StartGuestVerificationAsync(body, ct)))
            .RequireRateLimiting("sensitive");
        auth.MapPost("/guest/verify", (GuestVerifyRequest body, AuthService service, CancellationToken ct)
            => Handle(() => service.VerifyGuestAsync(body, ct)))
            .RequireRateLimiting("sensitive");

        app.MapGet("/api/users/me", async (ClaimsPrincipal principal, AuthService service, CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return AuthHttp.Unauthorized("Unauthorized");
            }

            return AuthHttp.From(await service.GetCurrentUserAsync(userId, ct));
        }).RequireAuthorization().WithTags("Users");

        app.MapPatch("/api/users/me", async (
            ClaimsPrincipal principal,
            UpdateProfileRequest body,
            AuthService service,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return AuthHttp.Unauthorized("Unauthorized");
            }

            return AuthHttp.From(await service.UpdateProfileAsync(userId, body, ct));
        }).RequireAuthorization().WithTags("Users");
    }

    private static async Task<IResult> Handle(Func<Task<AuthResult>> action)
        => AuthHttp.From(await action());

    internal static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(raw, out userId);
    }

    internal static class AuthHttp
    {
        public static IResult From(AuthResult result) => result.Status switch
        {
            AuthStatus.Ok when result.Tokens is not null => Results.Ok(result.Tokens),
            AuthStatus.Ok when result.User is not null => Results.Ok(result.User),
            AuthStatus.Ok when result.DemoCode is not null
                => Results.Ok(new { sent = true, demoCode = result.DemoCode }),
            AuthStatus.Ok => Results.Ok(new { sent = true }),
            AuthStatus.NoMatch => Results.Ok(new { matched = false }),
            AuthStatus.Created when result.Tokens is not null => Results.Created("/api/users/me", result.Tokens),
            AuthStatus.NoContent => Results.NoContent(),
            AuthStatus.Validation => Results.Json(
                new { error = result.Error, details = result.Details ?? [] },
                Json,
                statusCode: StatusCodes.Status400BadRequest),
            AuthStatus.DuplicateEmail => Results.Json(
                new { error = result.Error, details = result.Details ?? [] },
                Json,
                statusCode: StatusCodes.Status409Conflict),
            AuthStatus.Unauthorized => Unauthorized(result.Error ?? "Unauthorized"),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };

        public static IResult Unauthorized(string error)
            => Results.Json(new { error, details = Array.Empty<string>() }, Json, statusCode: StatusCodes.Status401Unauthorized);
    }
}
