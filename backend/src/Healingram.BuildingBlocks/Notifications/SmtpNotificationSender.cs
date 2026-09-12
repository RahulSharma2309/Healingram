using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Healingram.BuildingBlocks.Notifications;

public sealed class SmtpSettings
{
    public required string Host { get; init; }
    public int Port { get; init; } = 1025;
    public required string From { get; init; }
    public required string To { get; init; }

    public static SmtpSettings FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Smtp");
        var host = string.IsNullOrWhiteSpace(section["Host"]) ? "localhost" : section["Host"]!;
        var from = string.IsNullOrWhiteSpace(section["From"]) ? "healingram@localhost" : section["From"]!;
        var to = string.IsNullOrWhiteSpace(section["To"]) ? from : section["To"]!;
        var port = 1025;
        if (int.TryParse(section["Port"], out var parsed) && parsed > 0)
        {
            port = parsed;
        }

        return new SmtpSettings
        {
            Host = host,
            Port = port,
            From = from,
            To = to
        };
    }
}

internal sealed class SmtpNotificationSender(SmtpSettings settings, ILogger<SmtpNotificationSender> logger)
    : INotificationSender
{
    public async Task SendAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = false
        };

        using var mail = new MailMessage(settings.From, settings.To)
        {
            Subject = $"Healingram {message.Kind}",
            Body = message.PayloadJson,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
        mail.Headers.Add("X-Healingram-Kind", message.Kind);
        mail.Headers.Add("X-Healingram-Outbox-Id", message.Id.ToString("D"));

        await client.SendMailAsync(mail, cancellationToken);
        logger.LogInformation("SMTP accepted outbox {OutboxId} kind {Kind}", message.Id, message.Kind);
    }
}
