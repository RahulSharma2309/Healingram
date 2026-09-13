using System.Text.Json;
using Healingram.Modules.Leads.Application;
using Healingram.Modules.Leads.Domain;
using Healingram.Modules.Leads.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Healingram.Modules.Leads.Tests;

public class LeadCaptureTests
{
    [Fact]
    public async Task Create_persists_new_lead_and_status_history_when_whatsapp_consent_is_false()
    {
        var (service, store) = LeadHarness.Create();

        var result = await service.CreateAsync(LeadHarness.Request(whatsappConsent: false), CancellationToken.None);

        Assert.Equal(LeadOutcomeKind.Created, result.Kind);
        Assert.NotNull(result.Entity);
        Assert.Equal(LeadStatuses.New, result.Entity.Status);
        Assert.False(result.Entity.WhatsappConsent);
        Assert.Equal("+919876543210", result.Entity.PhoneE164);
        Assert.Contains("\"helpType\":\"choosing_retreat\"", result.Entity.ContextJson, StringComparison.Ordinal);
        Assert.Contains("\"need\":\"stress_burnout\"", result.Entity.ContextJson, StringComparison.Ordinal);
        Assert.Contains("\"travelWindow\":\"this_month\"", result.Entity.ContextJson, StringComparison.Ordinal);

        var history = Assert.Single(result.Entity.History);
        Assert.Null(history.FromStatus);
        Assert.Equal(LeadStatuses.New, history.ToStatus);

        var stored = Assert.Single(store.All);
        Assert.Equal(result.Entity.Id, stored.Id);
        Assert.Equal(LeadStatuses.New, stored.Status);
        Assert.Single(stored.History);

        var http = await Read(LeadsEndpoints.Handle(Task.FromResult(result)));
        Assert.Equal(StatusCodes.Status201Created, http.Status);
        Assert.Equal(result.Entity.Id.ToString(), http.Body.GetProperty("id").GetString());
        Assert.Equal(LeadStatuses.New, http.Body.GetProperty("status").GetString());
        Assert.False(http.Body.TryGetProperty("fullName", out _));
    }

    [Fact]
    public async Task Create_returns_400_and_does_not_persist_when_contact_details_are_invalid()
    {
        var (service, store) = LeadHarness.Create();

        var result = await service.CreateAsync(
            LeadHarness.Request(fullName: " ", phone: "12", email: "not-an-email"),
            CancellationToken.None);

        Assert.Equal(LeadOutcomeKind.Validation, result.Kind);
        Assert.True(result.Details!.Count >= 3);
        Assert.All(result.Details, detail =>
        {
            Assert.DoesNotContain("diagnos", detail, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("patient", detail, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Empty(store.All);

        var http = await Read(LeadsEndpoints.Handle(Task.FromResult(result)));
        Assert.Equal(StatusCodes.Status400BadRequest, http.Status);
        Assert.Equal("Validation failed", http.Body.GetProperty("error").GetString());
        Assert.True(http.Body.GetProperty("details").GetArrayLength() >= 3);
    }

    [Fact]
    public async Task Get_returns_persisted_lead()
    {
        var (service, _) = LeadHarness.Create();
        var created = await service.CreateAsync(LeadHarness.Request(), CancellationToken.None);

        var result = await service.GetAsync(created.Entity!.Id, CancellationToken.None);

        Assert.Equal(LeadOutcomeKind.Ok, result.Kind);
        Assert.Equal(created.Entity.Id, result.Entity!.Id);
        Assert.Equal(LeadStatuses.New, result.Entity.Status);

        var http = await Read(LeadsEndpoints.Handle(Task.FromResult(result)));
        Assert.Equal(StatusCodes.Status200OK, http.Status);
        Assert.Equal(created.Entity.Id.ToString(), http.Body.GetProperty("id").GetString());
        Assert.Equal("Guest Local", http.Body.GetProperty("fullName").GetString());
        Assert.Equal("choosing_retreat", http.Body.GetProperty("helpType").GetString());
    }

    [Fact]
    public async Task Get_missing_lead_returns_not_found()
    {
        var (service, _) = LeadHarness.Create();

        var result = await service.GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(LeadOutcomeKind.NotFound, result.Kind);

        var http = await Read(LeadsEndpoints.Handle(Task.FromResult(result)));
        Assert.Equal(StatusCodes.Status404NotFound, http.Status);
        Assert.Equal("Not found", http.Body.GetProperty("error").GetString());
    }

    private static async Task<(int Status, JsonElement Body)> Read(Task<IResult> action)
    {
        var result = await action;
        var status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        var value = Assert.IsAssignableFrom<IValueHttpResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value.Value, LeadJson.Options));
        return (status.StatusCode ?? StatusCodes.Status200OK, document.RootElement.Clone());
    }
}
