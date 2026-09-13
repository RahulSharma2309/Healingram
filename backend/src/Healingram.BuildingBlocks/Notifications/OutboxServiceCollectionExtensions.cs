using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Healingram.BuildingBlocks.Notifications;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddHealingramOutbox(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(SmtpSettings.FromConfiguration(configuration));
        services.AddSingleton<INotificationSender, SmtpNotificationSender>();
        services.AddScoped<IOutboxStore, PostgresOutboxStore>();
        services.AddScoped<INotificationOutbox, NotificationOutbox>();
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<OutboxDispatchHostedService>();
        return services;
    }
}
