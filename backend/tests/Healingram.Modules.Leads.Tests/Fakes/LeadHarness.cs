using Healingram.Modules.Leads.Application;
using Microsoft.Extensions.Logging.Abstractions;

namespace Healingram.Modules.Leads.Tests.Fakes;

internal static class LeadHarness
{
    public static (LeadService Service, InMemoryLeadStore Store) Create()
    {
        var store = new InMemoryLeadStore();
        var service = new LeadService(store, TimeProvider.System, NullLogger<LeadService>.Instance);
        return (service, store);
    }

    public static CreateLeadRequest Request(
        string? fullName = "Guest Local",
        string? phone = "919876543210",
        string? email = "guest@local.test",
        string? helpType = "choosing_retreat",
        string? need = "stress_burnout",
        string? travelWindow = "this_month",
        bool? whatsappConsent = false,
        string? source = "contact")
        => new()
        {
            FullName = fullName,
            Phone = phone,
            Email = email,
            HelpType = helpType,
            Need = need,
            TravelWindow = travelWindow,
            WhatsappConsent = whatsappConsent,
            Source = source
        };
}
