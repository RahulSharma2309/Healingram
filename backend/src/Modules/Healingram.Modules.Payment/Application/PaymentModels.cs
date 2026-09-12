namespace Healingram.Modules.Payment.Application;

internal static class PaymentStatuses
{
    public const string Ready = "ready";
    public const string Paid = "paid";
}

internal static class PaymentProviders
{
    public const string Fake = "fake";
}

internal static class PaymentWebhookHeaders
{
    public const string Secret = "X-Webhook-Secret";
}

internal sealed class PaymentSettings
{
    public const string DefaultFakeWebhookSecret = "local-dev-webhook-secret";

    public string FakeWebhookSecret { get; init; } = DefaultFakeWebhookSecret;

    public static PaymentSettings From(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var secret = configuration["Payment:FakeWebhookSecret"];
        return new PaymentSettings
        {
            FakeWebhookSecret = string.IsNullOrWhiteSpace(secret) ? DefaultFakeWebhookSecret : secret
        };
    }
}

internal sealed record CreatePaymentIntentRequest(string? PublicId, string? IdempotencyKey);

internal sealed record FakeWebhookRequest(Guid? IntentId, string? ProviderEventId);

internal sealed class PaymentIntentEntity
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public required string Provider { get; init; }
    public string? ProviderRef { get; init; }
    public decimal AmountInr { get; init; }
    public required string Currency { get; init; }
    public required string Status { get; set; }
    public required string IdempotencyKey { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

internal enum PaymentOutcomeKind
{
    Created,
    Ok,
    Replayed,
    Conflict,
    Validation,
    NotFound,
    Unauthorized,
    Forbidden
}

internal sealed record PaymentOutcome(
    PaymentOutcomeKind Kind,
    PaymentIntentEntity? Entity = null,
    string? Error = null,
    IReadOnlyList<string>? Details = null)
{
    public static PaymentOutcome Created(PaymentIntentEntity entity)
        => new(PaymentOutcomeKind.Created, entity);

    public static PaymentOutcome Ok(PaymentIntentEntity entity)
        => new(PaymentOutcomeKind.Ok, entity);

    public static PaymentOutcome Replayed(PaymentIntentEntity entity)
        => new(PaymentOutcomeKind.Replayed, entity);

    public static PaymentOutcome Conflict(PaymentIntentEntity entity)
        => new(PaymentOutcomeKind.Conflict, entity, "Idempotency conflict");

    public static PaymentOutcome Invalid(params string[] details)
        => new(PaymentOutcomeKind.Validation, Error: "Validation failed", Details: details);

    public static PaymentOutcome Missing()
        => new(PaymentOutcomeKind.NotFound, Error: "Not found");

    public static PaymentOutcome Unauthorized(string detail)
        => new(PaymentOutcomeKind.Unauthorized, Error: "Unauthorized", Details: [detail]);

    public static PaymentOutcome Forbidden(string detail)
        => new(PaymentOutcomeKind.Forbidden, Error: "Forbidden", Details: [detail]);
}

internal sealed class DuplicatePaymentIdempotencyException : Exception;
