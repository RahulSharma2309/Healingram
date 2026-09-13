namespace Healingram.Contracts.Payment;

public static class PaymentProviders
{
    public const string Local = "local";
    public const string Fake = "fake";
    public const string Razorpay = "razorpay";
    public const string Stripe = "stripe";

    public static string Normalize(string? configured)
        => string.IsNullOrWhiteSpace(configured) ? Local : configured.Trim().ToLowerInvariant();

    public static bool IsImplemented(string? configured)
        => Normalize(configured) is Local or Fake;
}

public static class NormalizedPaymentStatuses
{
    public const string Created = "created";
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string RefundPending = "refund_pending";
    public const string Refunded = "refunded";

    public static bool IsOpen(string status)
        => status is Created or Pending or Processing;

    public static bool IsPaid(string status)
        => status is Succeeded;
}

public sealed record CreateProviderPayment(
    Guid IntentId,
    Guid BookingId,
    decimal Amount,
    string Currency,
    string PublicId,
    string IdempotencyKey);

public sealed record ProviderPaymentRef(string Provider, string ProviderRef, string CheckoutPath);

public sealed record PaymentProviderEvent(
    string Provider,
    string ProviderEventId,
    Guid IntentId,
    Guid? BookingId,
    decimal? Amount,
    string? Currency,
    string NormalizedStatus,
    string RawPayload);

public sealed record PaymentWebhookVerifyResult(
    bool Accepted,
    PaymentProviderEvent? Event,
    string? Error,
    bool Unauthorized = false,
    bool Forbidden = false);

public interface IPaymentProvider
{
    string Name { get; }

    Task<ProviderPaymentRef> CreatePaymentAsync(CreateProviderPayment request, CancellationToken cancellationToken);

    PaymentWebhookVerifyResult VerifyWebhook(string? providedSecret, string rawBody);
}
