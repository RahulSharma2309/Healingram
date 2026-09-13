using System.Diagnostics;
using System.Text.Json;
using Healingram.BuildingBlocks.Notifications;
using Healingram.Contracts.Availability;
using Healingram.Contracts.Booking;
using Healingram.Contracts.Catalog;
using Healingram.Contracts.Identity;
using Healingram.Contracts.Partners;
using Healingram.Modules.Availability.Domain;
using Healingram.Modules.Availability.Persistence;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Availability.Application;

internal sealed class AvailabilityService(
    IAvailabilityStore store,
    ICatalogReadPort catalog,
    IBookingCommands bookings,
    IBookingPaymentPort bookingPayments,
    IPartnerAccess partnerAccess,
    IGuestIdentityPort guests,
    TimeProvider clock,
    ILogger<AvailabilityService> logger,
    INotificationOutbox? outbox = null)
{
    public async Task<AvailabilityOutcome> CreateAsync(
        CreateAvailabilityRequest request,
        Actor actor,
        CancellationToken cancellationToken)
    {
        using var activity = AvailabilityTelemetry.Source.StartActivity("availability.create");

        var details = StayRules.Validate(
            request.IdempotencyKey,
            request.RetreatSlug,
            request.ProgrammeSlug,
            request.DurationNights,
            request.Occupancy,
            request.Guests,
            request.CheckIn,
            request.CustomerName,
            request.Email,
            request.Phone,
            out var stay);
        if (stay is null)
        {
            return AvailabilityOutcome.Invalid([.. details]);
        }

        var published = await catalog.GetPublicRetreatSlugsAsync(cancellationToken);
        if (!published.Any(slug => slug.Equals(stay.RetreatSlug, StringComparison.OrdinalIgnoreCase)))
        {
            return AvailabilityOutcome.Invalid("retreat is not a published stay");
        }

        var existing = await store.FindByIdempotencyKeyAsync(stay.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return SameFingerprint(existing, stay)
                ? AvailabilityOutcome.Replayed(existing)
                : AvailabilityOutcome.Conflict(existing);
        }

        Guid? customerUserId = actor.UserId;
        if (customerUserId is null)
        {
            var guest = await guests.EnsureCustomerAsync(stay.Email, stay.Phone, stay.CustomerName, cancellationToken);
            if (guest.RequiresSignIn)
            {
                return AvailabilityOutcome.Invalid("You already have a Healingram account. Please sign in to continue.");
            }

            customerUserId = guest.UserId;
        }

        var now = clock.GetUtcNow();
        var year = now.Year;
        var sequence = await store.NextPublicSequenceAsync(cancellationToken);
        var entity = new AvailabilityRequestEntity
        {
            Id = Guid.NewGuid(),
            PublicId = PublicIds.Request(year, sequence),
            CustomerUserId = customerUserId,
            CustomerName = stay.CustomerName,
            CustomerEmail = stay.Email,
            CustomerPhone = stay.Phone,
            RetreatId = Guid.NewGuid(),
            ProgrammeId = Guid.NewGuid(),
            RetreatSlug = stay.RetreatSlug,
            ProgrammeSlug = stay.ProgrammeSlug,
            Status = AvailabilityStatuses.Requested,
            SnapshotJson = PriceSnapshotFactory.Capture(stay, now),
            IdempotencyKey = stay.IdempotencyKey,
            RequestedAt = now,
            History =
            [
                new StatusHistoryEntry(
                    Guid.NewGuid(),
                    null,
                    AvailabilityStatuses.Requested,
                    actor.Role,
                    actor.UserId,
                    null,
                    now)
            ]
        };

        try
        {
            await store.InsertAsync(entity, cancellationToken);
        }
        catch (DuplicateIdempotencyException)
        {
            var raced = await store.FindByIdempotencyKeyAsync(stay.IdempotencyKey, cancellationToken);
            if (raced is null)
            {
                return AvailabilityOutcome.Invalid("Could not create availability request");
            }

            return SameFingerprint(raced, stay)
                ? AvailabilityOutcome.Replayed(raced)
                : AvailabilityOutcome.Conflict(raced);
        }

        activity?.SetTag("availability.public_id", entity.PublicId);
        logger.LogInformation("Availability request {PublicId} created", entity.PublicId);
        await EnqueueSafe(
            NotificationKinds.AvailabilityRequested,
            $"availability-requested:{entity.PublicId}",
            new { publicId = entity.PublicId },
            cancellationToken);
        return AvailabilityOutcome.Created(entity);
    }

    public async Task<AvailabilityOutcome> GetAsync(
        string publicId,
        Actor actor,
        bool includeInternalNotes,
        CancellationToken cancellationToken)
    {
        var entity = await store.FindByPublicIdAsync(publicId, cancellationToken);
        if (entity is null)
        {
            return AvailabilityOutcome.Missing();
        }

        if (!await CanReadAsync(actor, entity, cancellationToken))
        {
            return actor.UserId is null
                ? AvailabilityOutcome.Unauth("Verify it's you to view this request")
                : AvailabilityOutcome.Deny("Not your request");
        }

        _ = includeInternalNotes;
        return AvailabilityOutcome.Ok(entity);
    }

    private async Task<bool> CanReadAsync(
        Actor actor,
        AvailabilityRequestEntity entity,
        CancellationToken cancellationToken)
    {
        if (actor.IsAdminWrite)
        {
            return true;
        }

        if (actor.IsPartnerWrite)
        {
            return actor.UserId is not null
                   && await partnerAccess.CanAccessRetreatAsync(
                       actor.UserId.Value,
                       entity.RetreatSlug,
                       cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(actor.ScopedRequestId)
            && !string.Equals(actor.ScopedRequestId, entity.PublicId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return actor.UserId is not null
               && entity.CustomerUserId is not null
               && actor.UserId == entity.CustomerUserId;
    }

    public Task<AvailabilityOutcome> ConfirmAsync(
        string publicId,
        ConfirmAvailabilityRequest body,
        Actor actor,
        CancellationToken cancellationToken)
        => TransitionAsync(
            publicId,
            actor,
            requirePartnerWrite: true,
            AvailabilityActions.Confirm,
            AvailabilityStatuses.Confirmed,
            reason: null,
            alternativeJson: null,
            finalAmountInr: body.FinalAmountInr,
            createBooking: true,
            cancellationToken);

    public Task<AvailabilityOutcome> OfferAlternativeAsync(
        string publicId,
        AlternativeAvailabilityRequest body,
        Actor actor,
        CancellationToken cancellationToken)
    {
        if (body.Proposal.ValueKind is not JsonValueKind.Object)
        {
            return Task.FromResult(AvailabilityOutcome.Invalid("proposal is required"));
        }

        return TransitionAsync(
            publicId,
            actor,
            requirePartnerWrite: true,
            AvailabilityActions.Alternative,
            AvailabilityStatuses.AlternativeOffered,
            reason: null,
            alternativeJson: body.Proposal.GetRawText(),
            finalAmountInr: TryAmount(body.Proposal),
            createBooking: false,
            cancellationToken);
    }

    public Task<AvailabilityOutcome> MarkUnavailableAsync(
        string publicId,
        UnavailableAvailabilityRequest body,
        Actor actor,
        CancellationToken cancellationToken)
    {
        var reason = body.Reason?.Trim() ?? "";
        if (reason.Length == 0)
        {
            return Task.FromResult(AvailabilityOutcome.Invalid("reason is required"));
        }

        return TransitionAsync(
            publicId,
            actor,
            requirePartnerWrite: true,
            AvailabilityActions.Unavailable,
            AvailabilityStatuses.Unavailable,
            reason,
            alternativeJson: null,
            finalAmountInr: null,
            createBooking: false,
            cancellationToken);
    }

    public Task<AvailabilityOutcome> AcceptAlternativeAsync(
        string publicId,
        Actor actor,
        CancellationToken cancellationToken)
        => TransitionAsync(
            publicId,
            actor,
            requirePartnerWrite: false,
            AvailabilityActions.AcceptAlternative,
            AvailabilityStatuses.Confirmed,
            reason: null,
            alternativeJson: null,
            finalAmountInr: null,
            createBooking: true,
            cancellationToken);

    public async Task<AvailabilityOutcome> AddAdminNoteAsync(
        string publicId,
        AdminNoteRequest body,
        Actor actor,
        CancellationToken cancellationToken)
    {
        if (!actor.IsAdminWrite)
        {
            return AvailabilityOutcome.Deny("AdminWrite required");
        }

        var note = body.Note?.Trim() ?? "";
        if (note.Length == 0)
        {
            return AvailabilityOutcome.Invalid("note is required");
        }

        var entity = await store.FindByPublicIdAsync(publicId, cancellationToken);
        if (entity is null)
        {
            return AvailabilityOutcome.Missing();
        }

        var entry = new AdminNoteEntry(Guid.NewGuid(), note, actor.UserId, clock.GetUtcNow());
        await store.AddAdminNoteAsync(entity.Id, entry, cancellationToken);
        entity.InternalNotes.Add(entry);
        logger.LogInformation("Admin note added on {PublicId}", entity.PublicId);
        return AvailabilityOutcome.Ok(entity);
    }

    public async Task<IReadOnlyList<AvailabilityRequestEntity>> ListPartnerPendingAsync(
        Actor actor,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string>? allowedSlugs = null;
        if (!actor.IsAdminWrite)
        {
            if (actor.UserId is null)
            {
                return [];
            }

            allowedSlugs = await partnerAccess.ListRetreatSlugsForUserAsync(actor.UserId.Value, cancellationToken);
            if (allowedSlugs.Count == 0)
            {
                return [];
            }
        }

        var items = await store.ListByStatusesAsync([AvailabilityStatuses.Requested], cancellationToken);
        if (allowedSlugs is not null)
        {
            var allowed = allowedSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
            items = items.Where(i => allowed.Contains(i.RetreatSlug)).ToArray();
        }

        var unseen = items.Where(i => i.PartnerViewedAt is null).Select(i => i.Id).ToArray();
        if (unseen.Length > 0)
        {
            var viewed = clock.GetUtcNow();
            await store.MarkPartnerViewedAsync(unseen, viewed, cancellationToken);
            foreach (var item in items.Where(i => i.PartnerViewedAt is null))
            {
                item.PartnerViewedAt = viewed;
            }
        }

        return items;
    }

    public async Task<IReadOnlyList<AvailabilityRequestEntity>> ListMineAsync(
        Actor actor,
        CancellationToken cancellationToken)
    {
        using var activity = AvailabilityTelemetry.Source.StartActivity("availability.list_mine");
        if (actor.UserId is null)
        {
            return [];
        }

        if (actor.IsGuestRequest)
        {
            return await ListGuestScopedAsync(actor, cancellationToken);
        }

        activity?.SetTag("availability.customer_scoped", true);
        var items = await store.ListByCustomerUserIdAsync(actor.UserId.Value, cancellationToken);
        logger.LogInformation("Listed {Count} requests for a customer", items.Count);
        return items;
    }

    public async Task<TripGroupsDto> ListTripsAsync(Actor actor, CancellationToken cancellationToken)
    {
        using var activity = AvailabilityTelemetry.Source.StartActivity("availability.list_trips");
        if (actor.UserId is null)
        {
            return TripGroupsDto.Empty;
        }

        activity?.SetTag("availability.customer_scoped", true);
        IReadOnlyList<AvailabilityRequestEntity> items = actor.IsGuestRequest
            ? await ListGuestScopedAsync(actor, cancellationToken)
            : await store.ListByCustomerUserIdAsync(actor.UserId.Value, cancellationToken);
        var confirmed = items
            .Where(i => string.Equals(i.Status, AvailabilityStatuses.Confirmed, StringComparison.Ordinal))
            .ToArray();
        var paidIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in confirmed)
        {
            var gate = await bookingPayments.FindByPublicIdAsync(item.PublicId, cancellationToken);
            if (gate is not null && string.Equals(gate.Status, BookingStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            {
                paidIds.Add(item.PublicId);
            }
        }

        var paymentPending = confirmed.Where(i => !paidIds.Contains(i.PublicId)).Select(ToTripCard).ToArray();
        var upcoming = confirmed
            .Where(i => paidIds.Contains(i.PublicId))
            .Select(i => ToTripCard(i) with { Status = BookingStatuses.Paid })
            .ToArray();
        var cancelled = items
            .Where(i => string.Equals(i.Status, AvailabilityStatuses.Unavailable, StringComparison.Ordinal))
            .Select(ToTripCard)
            .ToArray();

        logger.LogInformation(
            "Listed trips paymentPending={Pending} upcoming={Upcoming} cancelled={Cancelled}",
            paymentPending.Length,
            upcoming.Length,
            cancelled.Length);
        return new TripGroupsDto(paymentPending, upcoming, [], cancelled);
    }

    private async Task<IReadOnlyList<AvailabilityRequestEntity>> ListGuestScopedAsync(
        Actor actor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actor.ScopedRequestId))
        {
            return [];
        }

        var one = await store.FindByPublicIdAsync(actor.ScopedRequestId, cancellationToken);
        if (one is null || !await CanReadAsync(actor, one, cancellationToken))
        {
            return [];
        }

        return [one];
    }

    public Task<IReadOnlyList<AvailabilityRequestEntity>> ListAdminPendingAsync(CancellationToken cancellationToken)
        => store.ListByStatusesAsync(
            [AvailabilityStatuses.Requested, AvailabilityStatuses.AlternativeOffered],
            cancellationToken);

    private async Task<AvailabilityOutcome> TransitionAsync(
        string publicId,
        Actor actor,
        bool requirePartnerWrite,
        string action,
        string toStatus,
        string? reason,
        string? alternativeJson,
        decimal? finalAmountInr,
        bool createBooking,
        CancellationToken cancellationToken)
    {
        using var activity = AvailabilityTelemetry.Source.StartActivity($"availability.{action}");
        activity?.SetTag("availability.public_id", publicId);

        if (requirePartnerWrite && !actor.IsPartnerWrite)
        {
            return AvailabilityOutcome.Deny("PartnerWrite required");
        }

        var entity = await store.FindByPublicIdAsync(publicId, cancellationToken);
        if (entity is null)
        {
            return AvailabilityOutcome.Missing();
        }

        if (requirePartnerWrite && !actor.IsAdminWrite)
        {
            if (actor.UserId is null
                || !await partnerAccess.CanAccessRetreatAsync(actor.UserId.Value, entity.RetreatSlug, cancellationToken))
            {
                return AvailabilityOutcome.Deny("Not your retreat");
            }
        }

        if (!requirePartnerWrite && action == AvailabilityActions.AcceptAlternative && !await CanReadAsync(actor, entity, cancellationToken))
        {
            return actor.UserId is null
                ? AvailabilityOutcome.Unauth("Verify it's you to continue")
                : AvailabilityOutcome.Deny("Not your request");
        }

        var snapshotBefore = entity.SnapshotJson;
        var reject = StatusMachine.RejectReason(entity.Status, toStatus, action);
        if (reject is not null)
        {
            return AvailabilityOutcome.Illegal(reject);
        }

        if (action == AvailabilityActions.AcceptAlternative && string.IsNullOrWhiteSpace(entity.AlternativeJson))
        {
            return AvailabilityOutcome.Illegal("No alternative to accept");
        }

        if (createBooking)
        {
            var amount = finalAmountInr ?? entity.FinalAmountInr ?? TryAmountJson(entity.AlternativeJson);
            using var bookingSpan = AvailabilityTelemetry.Source.StartActivity("availability.create_booking");
            bookingSpan?.SetTag("availability.public_id", entity.PublicId);
            await bookings.CreateAwaitingPaymentAsync(
                new CreateAwaitingPaymentBooking(
                    entity.Id,
                    entity.PublicId,
                    entity.SnapshotJson,
                    amount,
                    entity.CustomerUserId),
                cancellationToken);
            finalAmountInr = amount;
        }

        var now = clock.GetUtcNow();
        var from = entity.Status;
        entity.Status = toStatus;
        entity.PartnerRespondedAt = requirePartnerWrite ? now : entity.PartnerRespondedAt;
        if (finalAmountInr is not null)
        {
            entity.FinalAmountInr = finalAmountInr;
        }

        if (alternativeJson is not null)
        {
            entity.AlternativeJson = alternativeJson;
        }

        if (!string.Equals(entity.SnapshotJson, snapshotBefore, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Price snapshot is immutable");
        }

        var history = new StatusHistoryEntry(
            Guid.NewGuid(),
            from,
            toStatus,
            actor.Role,
            actor.UserId,
            reason,
            now);
        entity.History.Add(history);
        await store.SavePartnerResponseAsync(entity, history, cancellationToken);

        logger.LogInformation(
            "Availability request {PublicId} transitioned {From} -> {To}",
            entity.PublicId,
            from,
            toStatus);

        if (string.Equals(toStatus, AvailabilityStatuses.Confirmed, StringComparison.Ordinal))
        {
            await EnqueueSafe(
                NotificationKinds.AvailabilityConfirmed,
                $"availability-confirmed:{entity.PublicId}",
                new { publicId = entity.PublicId },
                cancellationToken);
        }

        return AvailabilityOutcome.Ok(entity);
    }

    private async Task EnqueueSafe(string kind, string key, object payload, CancellationToken cancellationToken)
    {
        if (outbox is null)
        {
            return;
        }

        try
        {
            await outbox.EnqueueAsync(kind, key, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Outbox enqueue skipped for {Kind}", kind);
        }
    }

    private static bool SameFingerprint(AvailabilityRequestEntity entity, ValidatedStay stay)
        => StayRules.FingerprintFromStored(
               entity.SnapshotJson,
               entity.RetreatSlug,
               entity.ProgrammeSlug,
               entity.CustomerName,
               entity.CustomerEmail,
               entity.CustomerPhone,
               entity.IdempotencyKey)
           == StayRules.Fingerprint(stay);

    private static decimal? TryAmount(JsonElement proposal)
    {
        if (proposal.TryGetProperty("finalAmountInr", out var inr) && inr.TryGetDecimal(out var amount))
        {
            return amount;
        }

        if (proposal.TryGetProperty("finalAmount", out var alt) && alt.TryGetDecimal(out var legacy))
        {
            return legacy;
        }

        return null;
    }

    private static TripCardDto ToTripCard(AvailabilityRequestEntity entity)
        => new(
            entity.PublicId,
            entity.Status,
            entity.RetreatSlug,
            entity.ProgrammeSlug,
            entity.RequestedAt,
            entity.FinalAmountInr);

    private static decimal? TryAmountJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return TryAmount(document.RootElement);
    }
}

internal static class AvailabilityTelemetry
{
    public static readonly ActivitySource Source = new("Healingram.Availability");
}
