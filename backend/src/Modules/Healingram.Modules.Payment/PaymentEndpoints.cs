using System.Security.Claims;
using System.Text.Json;
using Healingram.Contracts.Identity;
using Healingram.Modules.Payment.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Healingram.Modules.Payment;

internal static class PaymentEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Map(IEndpointRouteBuilder app)
    {
        var payment = app.MapGroup("/api/payment").WithTags("Payment");

        payment.MapPost("/intents", async (
            CreatePaymentIntentRequest body,
            ClaimsPrincipal user,
            PaymentService service,
            CancellationToken cancellationToken) =>
        {
            if (IsGuestAccount(user))
            {
                return Results.Json(
                    new { error = "Create an account to continue to payment", details = Array.Empty<string>() },
                    Json,
                    statusCode: StatusCodes.Status403Forbidden);
            }

            return await Handle(service.CreateIntentAsync(body, ActorOf(user), cancellationToken));
        }).RequireAuthorization().RequireRateLimiting("sensitive");

        payment.MapGet("/intents/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            PaymentService service,
            CancellationToken cancellationToken)
            => await Handle(service.GetIntentAsync(id, ActorOf(user), cancellationToken)))
            .RequireAuthorization();

        payment.MapPost("/webhooks/local", (
            FakeWebhookRequest body,
            HttpRequest http,
            PaymentService service,
            PaymentSettings settings,
            CancellationToken cancellationToken)
            => LocalWebhook(body, http, service, settings, cancellationToken));

        payment.MapPost("/webhooks/fake", (
            FakeWebhookRequest body,
            HttpRequest http,
            PaymentService service,
            PaymentSettings settings,
            CancellationToken cancellationToken)
            => LocalWebhook(body, http, service, settings, cancellationToken));
    }

    private static Task<IResult> LocalWebhook(
        FakeWebhookRequest body,
        HttpRequest http,
        PaymentService service,
        PaymentSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.AllowLocalSimulate)
        {
            return Task.FromResult(Results.NotFound());
        }

        var secret = http.Headers[PaymentWebhookHeaders.Secret].ToString();
        return Handle(service.HandleFakeWebhookAsync(
            string.IsNullOrEmpty(secret) ? null : secret,
            body,
            cancellationToken));
    }

    private static PaymentActor ActorOf(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        Guid? userId = Guid.TryParse(raw, out var parsed) ? parsed : null;
        var guest = string.Equals(
            user.FindFirstValue("account_status"),
            AccountStatuses.Guest,
            StringComparison.OrdinalIgnoreCase);
        return new PaymentActor(userId, guest, RoleAuthorization.CanAuthorizeAdminWrite(user));
    }

    private static async Task<IResult> Handle(Task<PaymentOutcome> action)
    {
        var result = await action;
        return result.Kind switch
        {
            PaymentOutcomeKind.Created when result.Entity is not null
                => Results.Json(ToDto(result.Entity), Json, statusCode: StatusCodes.Status201Created),
            PaymentOutcomeKind.Ok or PaymentOutcomeKind.Replayed when result.Entity is not null
                => Results.Json(ToDto(result.Entity), Json),
            PaymentOutcomeKind.Conflict when result.Entity is not null
                => Results.Json(ToDto(result.Entity), Json, statusCode: StatusCodes.Status409Conflict),
            PaymentOutcomeKind.Validation
                => Results.Json(new { error = result.Error, details = result.Details ?? [] }, Json, statusCode: StatusCodes.Status400BadRequest),
            PaymentOutcomeKind.NotFound
                => Results.Json(new { error = result.Error, details = Array.Empty<string>() }, Json, statusCode: StatusCodes.Status404NotFound),
            PaymentOutcomeKind.Unauthorized
                => Results.Json(new { error = result.Error, details = result.Details ?? [] }, Json, statusCode: StatusCodes.Status401Unauthorized),
            PaymentOutcomeKind.Forbidden
                => Results.Json(new { error = result.Error, details = result.Details ?? [] }, Json, statusCode: StatusCodes.Status403Forbidden),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private static bool IsGuestAccount(ClaimsPrincipal user)
        => string.Equals(
            user.FindFirstValue("account_status"),
            AccountStatuses.Guest,
            StringComparison.OrdinalIgnoreCase);

    internal static object ToDto(PaymentIntentEntity entity)
        => new
        {
            id = entity.Id,
            status = entity.Status,
            checkoutUrl = string.Equals(entity.Status, PaymentStatuses.Ready, StringComparison.Ordinal)
                ? $"/pay/local/{entity.Id}"
                : null
        };
}
