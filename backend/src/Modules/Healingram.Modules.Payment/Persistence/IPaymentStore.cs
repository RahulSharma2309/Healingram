using Healingram.Modules.Payment.Application;

namespace Healingram.Modules.Payment.Persistence;

internal interface IPaymentStore
{
    Task<PaymentIntentEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PaymentIntentEntity?> FindByIdempotencyKeyAsync(string key, CancellationToken cancellationToken);
    Task InsertAsync(PaymentIntentEntity entity, CancellationToken cancellationToken);
    Task MarkIntentPaidAsync(Guid intentId, CancellationToken cancellationToken);
    Task<bool> TryInsertWebhookEventAsync(
        Guid id,
        string provider,
        string providerEventId,
        string payloadJson,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);
}
