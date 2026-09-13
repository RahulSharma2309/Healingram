using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Healingram.BuildingBlocks.Notifications;

public sealed class OutboxDispatcher(
    IOutboxStore store,
    INotificationSender sender,
    TimeProvider time,
    ILogger<OutboxDispatcher> logger)
{
    public const int DefaultBatchSize = 20;

    public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await store.ListPendingAsync(DefaultBatchSize, cancellationToken);
        foreach (var message in pending)
        {
            try
            {
                await sender.SendAsync(message, cancellationToken);
                await store.MarkSentAsync(message.Id, time.GetUtcNow(), cancellationToken);
                logger.LogInformation("Outbox sent {Kind} {OutboxId}", message.Kind, message.Id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await store.MarkFailedAsync(message.Id, cancellationToken);
                logger.LogWarning("Outbox failed {Kind} {OutboxId} ({Reason})", message.Kind, message.Id, ex.GetType().Name);
            }
        }
    }
}

internal sealed class OutboxDispatchHostedService(
    IServiceScopeFactory scopes,
    IHostEnvironment environment,
    ILogger<OutboxDispatchHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (environment.IsEnvironment("Testing"))
        {
            logger.LogInformation("Outbox dispatcher skipped in Testing environment");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
                await dispatcher.DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Outbox poll skipped ({Reason})", ex.GetType().Name);
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
