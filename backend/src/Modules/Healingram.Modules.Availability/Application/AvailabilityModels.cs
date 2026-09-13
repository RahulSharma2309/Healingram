using System.Text.Json;
using Healingram.Contracts.Identity;

namespace Healingram.Modules.Availability.Application;

internal sealed record CreateAvailabilityRequest(
    string? IdempotencyKey,
    string? RetreatSlug,
    string? ProgrammeSlug,
    int? DurationNights,
    string? Occupancy,
    int? Guests,
    string? CheckIn,
    string? CustomerName,
    string? Email,
    string? Phone);

internal sealed record ConfirmAvailabilityRequest(decimal? FinalAmountInr);

internal sealed record AlternativeAvailabilityRequest(JsonElement Proposal);

internal sealed record UnavailableAvailabilityRequest(string? Reason);

internal sealed record AdminNoteRequest(string? Note);

internal sealed record Actor(
    string Role,
    Guid? UserId,
    string? Purpose = null,
    string? ScopedRequestId = null,
    IReadOnlyList<string>? Roles = null)
{
    public IReadOnlyList<string> EffectiveRoles
        => Roles is { Count: > 0 } listed ? listed : [Role];

    public bool IsPartnerWrite => RoleAuthorization.SatisfiesPartnerWrite(EffectiveRoles);
    public bool IsAdminWrite => RoleAuthorization.SatisfiesAdminWrite(EffectiveRoles);
}

internal sealed class AvailabilityRequestEntity
{
    public Guid Id { get; init; }
    public required string PublicId { get; init; }
    public Guid? CustomerUserId { get; init; }
    public required string CustomerName { get; init; }
    public required string CustomerEmail { get; init; }
    public required string CustomerPhone { get; init; }
    public Guid RetreatId { get; init; }
    public Guid ProgrammeId { get; init; }
    public required string RetreatSlug { get; init; }
    public required string ProgrammeSlug { get; init; }
    public required string Status { get; set; }
    public required string SnapshotJson { get; init; }
    public required string IdempotencyKey { get; init; }
    public required DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? PartnerViewedAt { get; set; }
    public DateTimeOffset? PartnerRespondedAt { get; set; }
    public decimal? FinalAmountInr { get; set; }
    public string? AlternativeJson { get; set; }
    public List<StatusHistoryEntry> History { get; init; } = [];
    public List<AdminNoteEntry> InternalNotes { get; init; } = [];
}

internal sealed record StatusHistoryEntry(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string ActorRole,
    Guid? ActorId,
    string? Reason,
    DateTimeOffset OccurredAt);

internal sealed record AdminNoteEntry(Guid Id, string Body, Guid? ActorId, DateTimeOffset CreatedAt);

internal enum AvailabilityOutcomeKind
{
    Created,
    Ok,
    Replayed,
    Conflict,
    Validation,
    NotFound,
    IllegalTransition,
    Forbidden,
    Unauthorized
}

internal sealed record AvailabilityOutcome(
    AvailabilityOutcomeKind Kind,
    AvailabilityRequestEntity? Entity = null,
    string? Error = null,
    IReadOnlyList<string>? Details = null)
{
    public static AvailabilityOutcome Created(AvailabilityRequestEntity entity)
        => new(AvailabilityOutcomeKind.Created, entity);

    public static AvailabilityOutcome Ok(AvailabilityRequestEntity entity)
        => new(AvailabilityOutcomeKind.Ok, entity);

    public static AvailabilityOutcome Replayed(AvailabilityRequestEntity entity)
        => new(AvailabilityOutcomeKind.Replayed, entity);

    public static AvailabilityOutcome Conflict(AvailabilityRequestEntity entity)
        => new(AvailabilityOutcomeKind.Conflict, entity, "Idempotency conflict");

    public static AvailabilityOutcome Invalid(params string[] details)
        => new(AvailabilityOutcomeKind.Validation, Error: "Validation failed", Details: details);

    public static AvailabilityOutcome Missing()
        => new(AvailabilityOutcomeKind.NotFound, Error: "Not found");

    public static AvailabilityOutcome Illegal(string detail)
        => new(AvailabilityOutcomeKind.IllegalTransition, Error: "Illegal transition", Details: [detail]);

    public static AvailabilityOutcome Deny(string detail)
        => new(AvailabilityOutcomeKind.Forbidden, Error: "Forbidden", Details: [detail]);

    public static AvailabilityOutcome Unauth(string detail)
        => new(AvailabilityOutcomeKind.Unauthorized, Error: detail, Details: [detail]);
}

internal sealed class DuplicateIdempotencyException : Exception;

internal sealed record TripCardDto(
    string PublicId,
    string Status,
    string RetreatSlug,
    string ProgrammeSlug,
    DateTimeOffset RequestedAt,
    decimal? FinalAmountInr);

internal sealed record TripGroupsDto(
    IReadOnlyList<TripCardDto> PaymentPending,
    IReadOnlyList<TripCardDto> Upcoming,
    IReadOnlyList<TripCardDto> Completed,
    IReadOnlyList<TripCardDto> Cancelled)
{
    public static TripGroupsDto Empty { get; } = new([], [], [], []);
}
