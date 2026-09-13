using System.Text.Json;
using Healingram.Contracts.Payment;
using Healingram.Modules.Payment.Application;

namespace Healingram.Modules.Payment.Infrastructure;

internal sealed class LocalPaymentProvider(PaymentSettings settings) : IPaymentProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string Name => PaymentProviders.Local;

    public Task<ProviderPaymentRef> CreatePaymentAsync(
        CreateProviderPayment request,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Task.FromResult(new ProviderPaymentRef(
            Name,
            $"local_{request.IntentId:N}",
            $"/pay/local/{request.IntentId}"));
    }

    public PaymentWebhookVerifyResult VerifyWebhook(string? providedSecret, string rawBody)
    {
        if (string.IsNullOrEmpty(providedSecret))
        {
            return new PaymentWebhookVerifyResult(false, null, "Webhook secret is required", Unauthorized: true);
        }

        if (!PaymentService.SecretsEqual(providedSecret, settings.WebhookSecret))
        {
            return new PaymentWebhookVerifyResult(false, null, "Webhook secret is invalid", Forbidden: true);
        }

        LocalWebhookBody? body;
        try
        {
            body = JsonSerializer.Deserialize<LocalWebhookBody>(rawBody, Json);
        }
        catch (JsonException)
        {
            return new PaymentWebhookVerifyResult(false, null, "Webhook payload is not valid");
        }

        if (body?.IntentId is null || body.IntentId == Guid.Empty)
        {
            return new PaymentWebhookVerifyResult(false, null, "intentId is required");
        }

        var eventId = body.ProviderEventId?.Trim() ?? "";
        if (eventId.Length == 0)
        {
            return new PaymentWebhookVerifyResult(false, null, "providerEventId is required");
        }

        return new PaymentWebhookVerifyResult(
            true,
            new PaymentProviderEvent(
                Name,
                eventId,
                body.IntentId.Value,
                body.BookingId,
                body.AmountInr,
                string.IsNullOrWhiteSpace(body.Currency) ? "INR" : body.Currency,
                NormalizedPaymentStatuses.Succeeded,
                rawBody),
            null);
    }

    private sealed record LocalWebhookBody(
        Guid? IntentId,
        string? ProviderEventId,
        decimal? AmountInr,
        string? Currency,
        Guid? BookingId);
}
