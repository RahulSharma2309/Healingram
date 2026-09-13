using System.Text.Json;
using Healingram.Contracts.Availability;
using Healingram.Modules.Availability.Application;
using Healingram.Modules.Availability.Domain;
using Healingram.Modules.Availability.Tests.Fakes;
using Xunit;

namespace Healingram.Modules.Availability.Tests;

public class AvailabilityRequestTests
{
    [Fact]
    public async Task Create_assigns_human_readable_public_id_and_frozen_snapshot()
    {
        var (service, _, _) = AvailabilityHarness.Create();

        var result = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Created, result.Kind);
        Assert.NotNull(result.Entity);
        Assert.True(PublicIds.LooksHumanReadable(result.Entity.PublicId));
        Assert.Equal(AvailabilityStatuses.Requested, result.Entity.Status);
        Assert.Contains("\"priceStatus\":\"ON_REQUEST\"", result.Entity.SnapshotJson, StringComparison.Ordinal);
        Assert.Contains("\"checkIn\":\"2026-11-02\"", result.Entity.SnapshotJson, StringComparison.Ordinal);
        Assert.Contains("\"checkOut\":\"2026-11-09\"", result.Entity.SnapshotJson, StringComparison.Ordinal);
        Assert.Contains("\"retreatName\":\"Published Retreat\"", result.Entity.SnapshotJson, StringComparison.Ordinal);
        Assert.Contains("\"programmeName\":\"Panchakarma\"", result.Entity.SnapshotJson, StringComparison.Ordinal);
        Assert.DoesNotContain("+91", result.Entity.SnapshotJson, StringComparison.Ordinal);
        Assert.DoesNotContain("MARKETPLACE_SPLIT", result.Entity.SnapshotJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_rejects_unknown_programme()
    {
        var (service, _, _) = AvailabilityHarness.Create();

        var result = await service.CreateAsync(
            AvailabilityHarness.Request(programme: ""),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Validation, result.Kind);
    }

    [Fact]
    public async Task Create_rejects_unpublished_retreat()
    {
        var (service, _, _) = AvailabilityHarness.Create();

        var result = await service.CreateAsync(
            AvailabilityHarness.Request(retreat: "draft-retreat"),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Validation, result.Kind);
        Assert.Contains(result.Details!, d => d.Contains("published", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Create_validates_stay_fields()
    {
        var (service, _, _) = AvailabilityHarness.Create();

        var result = await service.CreateAsync(
            new CreateAvailabilityRequest("short", "", "", 0, "", 0, "not-a-date", "", "nope", "123"),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Validation, result.Kind);
        Assert.True(result.Details!.Count >= 6);
    }

    [Fact]
    public async Task Same_idempotency_key_and_payload_returns_original_row()
    {
        var (service, store, _) = AvailabilityHarness.Create();
        var first = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);
        var second = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Replayed, second.Kind);
        Assert.Equal(first.Entity!.PublicId, second.Entity!.PublicId);
        Assert.Equal(first.Entity.SnapshotJson, second.Entity.SnapshotJson);
        Assert.Single((await store.ListByStatusesAsync([AvailabilityStatuses.Requested], CancellationToken.None)));
    }

    [Fact]
    public async Task Same_idempotency_key_different_payload_returns_conflict_with_original()
    {
        var (service, _, _) = AvailabilityHarness.Create();
        var first = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);
        var second = await service.CreateAsync(
            AvailabilityHarness.Request(nights: 14),
            AvailabilityHarness.Customer,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Conflict, second.Kind);
        Assert.Equal(first.Entity!.PublicId, second.Entity!.PublicId);
        Assert.Equal(first.Entity.SnapshotJson, second.Entity.SnapshotJson);
    }

    [Fact]
    public async Task Snapshot_is_immutable_after_confirm_and_alternative()
    {
        var (service, _, bookings) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);
        var snapshot = created.Entity!.SnapshotJson;
        var publicId = created.Entity.PublicId;

        var confirmed = await service.ConfirmAsync(
            publicId,
            new ConfirmAvailabilityRequest(45000),
            AvailabilityHarness.Partner,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Ok, confirmed.Kind);
        Assert.Equal(snapshot, confirmed.Entity!.SnapshotJson);
        Assert.Equal(AvailabilityStatuses.Confirmed, confirmed.Entity.Status);
        Assert.Single(bookings.Calls);
        Assert.Equal(snapshot, bookings.Calls[0].SnapshotJson);

        var other = await service.CreateAsync(AvailabilityHarness.Request("idem-key-002"), AvailabilityHarness.Customer, CancellationToken.None);
        var otherSnapshot = other.Entity!.SnapshotJson;
        using var proposal = JsonDocument.Parse("""{"finalAmountInr":32000,"checkIn":"2026-12-01"}""");
        var alternative = await service.OfferAlternativeAsync(
            other.Entity.PublicId,
            new AlternativeAvailabilityRequest(proposal.RootElement.Clone()),
            AvailabilityHarness.Partner,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Ok, alternative.Kind);
        Assert.Equal(otherSnapshot, alternative.Entity!.SnapshotJson);
        Assert.Equal(AvailabilityStatuses.AlternativeOffered, alternative.Entity.Status);

        var accepted = await service.AcceptAlternativeAsync(
            other.Entity.PublicId,
            AvailabilityHarness.Customer,
            CancellationToken.None);
        Assert.Equal(AvailabilityOutcomeKind.Ok, accepted.Kind);
        Assert.Equal(otherSnapshot, accepted.Entity!.SnapshotJson);
        Assert.Equal(AvailabilityStatuses.Confirmed, accepted.Entity.Status);
        Assert.Equal(2, bookings.Calls.Count);
    }

    [Fact]
    public async Task Guest_get_keeps_admin_notes_off_the_dto()
    {
        var (service, _, _) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        await service.AddAdminNoteAsync(
            created.Entity!.PublicId,
            new AdminNoteRequest("internal follow-up"),
            AvailabilityHarness.Admin,
            CancellationToken.None);

        var loaded = (await service.GetAsync(
            created.Entity.PublicId,
            AvailabilityHarness.Admin,
            includeInternalNotes: true,
            CancellationToken.None)).Entity!;
        var guestJson = JsonSerializer.Serialize(AvailabilityEndpoints.ToDto(loaded, includeNotes: false));
        Assert.DoesNotContain("internal follow-up", guestJson, StringComparison.Ordinal);
        Assert.DoesNotContain("internalNotes", guestJson, StringComparison.Ordinal);

        var adminJson = JsonSerializer.Serialize(AvailabilityEndpoints.ToDto(loaded, includeNotes: true));
        Assert.Contains("internal follow-up", adminJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Customer_can_cancel_a_requested_stay()
    {
        var (service, _, _) = AvailabilityHarness.Create();
        var created = await service.CreateAsync(AvailabilityHarness.Request(), AvailabilityHarness.Customer, CancellationToken.None);

        var cancelled = await service.CancelAsync(
            created.Entity!.PublicId,
            AvailabilityHarness.Customer,
            CancellationToken.None);

        Assert.Equal(AvailabilityOutcomeKind.Ok, cancelled.Kind);
        Assert.Equal(AvailabilityStatuses.Cancelled, cancelled.Entity!.Status);
    }
}
