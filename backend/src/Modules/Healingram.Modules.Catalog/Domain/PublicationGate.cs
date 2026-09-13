namespace Healingram.Modules.Catalog.Domain;

public enum PriceStatus
{
    Verified = 1,
    Estimated = 2,
    OnRequest = 3
}

public enum RetreatPublicationStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2,
    PendingReview = 3,
    Approved = 4,
    Suspended = 5
}

public sealed record PublicationInput(
    RetreatPublicationStatus Status,
    string StateSlug,
    bool HasRequiredIdentity,
    int ValidProgrammeCount);

public static class PublicationGate
{
    /// <summary>
    /// Public = active + identity complete + at least one valid programme.
    /// Geography is inventory-driven and never blocks publication.
    /// </summary>
    public static bool IsPubliclyVisible(PublicationInput input)
    {
        if (input.Status != RetreatPublicationStatus.Active)
        {
            return false;
        }

        if (!input.HasRequiredIdentity)
        {
            return false;
        }

        return input.ValidProgrammeCount >= 1;
    }

    public static bool CanDisplayAsFact(PriceStatus status) => status == PriceStatus.Verified;
}
