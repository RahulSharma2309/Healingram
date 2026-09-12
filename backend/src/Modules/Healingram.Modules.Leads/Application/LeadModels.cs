using System.Text.Json;
using System.Text.Json.Serialization;

namespace Healingram.Modules.Leads.Application;

internal sealed class CreateLeadRequest
{
    public string? FullName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? HelpType { get; init; }
    public string? Need { get; init; }
    public string? TravelWindow { get; init; }
    public bool? WhatsappConsent { get; init; }
    public string? Source { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; init; }
}

internal sealed class LeadEntity
{
    public Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string PhoneE164 { get; init; }
    public required string Email { get; init; }
    public required bool WhatsappConsent { get; init; }
    public string? Source { get; init; }
    public required string Status { get; init; }
    public required string ContextJson { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public List<LeadStatusHistory> History { get; init; } = [];
}

internal sealed record LeadStatusHistory(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    Guid? ActorId,
    DateTimeOffset OccurredAt);

internal enum LeadOutcomeKind
{
    Created,
    Ok,
    Validation,
    NotFound
}

internal sealed record LeadOutcome(
    LeadOutcomeKind Kind,
    LeadEntity? Entity = null,
    string? Error = null,
    IReadOnlyList<string>? Details = null)
{
    public static LeadOutcome Created(LeadEntity entity)
        => new(LeadOutcomeKind.Created, entity);

    public static LeadOutcome Ok(LeadEntity entity)
        => new(LeadOutcomeKind.Ok, entity);

    public static LeadOutcome Invalid(params string[] details)
        => new(LeadOutcomeKind.Validation, Error: "Validation failed", Details: details);

    public static LeadOutcome Missing()
        => new(LeadOutcomeKind.NotFound, Error: "Not found");
}
