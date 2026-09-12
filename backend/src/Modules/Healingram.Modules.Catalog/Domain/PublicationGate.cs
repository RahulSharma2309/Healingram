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
    Archived = 2
}

public sealed record PublicationInput(
    RetreatPublicationStatus Status,
    string StateSlug,
    bool HasRequiredIdentity,
    int ValidProgrammeCount);

public static class PublicationGate
{
    public static readonly HashSet<string> V1States = new(StringComparer.OrdinalIgnoreCase)
    {
        "karnataka",
        "kerala"
    };

    public static readonly HashSet<string> ForbiddenPublicStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "goa",
        "rishikesh",
        "himachal-pradesh",
        "uttarakhand",
        "north-india"
    };

    public static bool IsPubliclyVisible(PublicationInput input)
    {
        if (input.Status != RetreatPublicationStatus.Active)
        {
            return false;
        }

        if (!V1States.Contains(input.StateSlug))
        {
            return false;
        }

        if (ForbiddenPublicStates.Contains(input.StateSlug))
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
