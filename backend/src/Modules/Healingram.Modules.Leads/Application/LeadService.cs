using System.Diagnostics;
using Healingram.Modules.Leads.Domain;
using Healingram.Modules.Leads.Persistence;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Leads.Application;

internal sealed class LeadService(
    ILeadStore store,
    TimeProvider clock,
    ILogger<LeadService> logger)
{
    public async Task<LeadOutcome> CreateAsync(CreateLeadRequest? request, CancellationToken cancellationToken)
    {
        using var activity = LeadTelemetry.Source.StartActivity("leads.create");

        var details = LeadRules.Validate(request, out var lead);
        if (lead is null)
        {
            return LeadOutcome.Invalid([.. details]);
        }

        var now = clock.GetUtcNow();
        var entity = new LeadEntity
        {
            Id = Guid.NewGuid(),
            FullName = lead.FullName,
            PhoneE164 = lead.PhoneE164,
            Email = lead.Email,
            WhatsappConsent = lead.WhatsappConsent,
            Source = lead.Source,
            Status = LeadStatuses.New,
            ContextJson = lead.ContextJson,
            CreatedAt = now,
            History =
            [
                new LeadStatusHistory(Guid.NewGuid(), null, LeadStatuses.New, null, now)
            ]
        };

        await store.InsertAsync(entity, cancellationToken);

        activity?.SetTag("leads.id", entity.Id.ToString());
        activity?.SetTag("leads.status", entity.Status);
        logger.LogInformation("Expert lead {LeadId} created", entity.Id);
        return LeadOutcome.Created(entity);
    }

    public async Task<LeadOutcome> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await store.FindByIdAsync(id, cancellationToken);
        return entity is null ? LeadOutcome.Missing() : LeadOutcome.Ok(entity);
    }
}

internal static class LeadTelemetry
{
    public static readonly ActivitySource Source = new("Healingram.Leads");
}
