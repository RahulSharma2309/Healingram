namespace Healingram.Contracts.Partners;

/// <summary>
/// Stored on <c>partners.partner_users.membership_role</c> and returned by
/// <c>GET /api/users/me</c>. V1 authorization is still <strong>active membership</strong>
/// plus retreat-slug access — these values are the foundation for later
/// owner / manager / finance / operations policies.
/// </summary>
public static class PartnerMembershipRoles
{
    public const string Owner = "owner";
    public const string Manager = "manager";
    public const string Finance = "finance";
    public const string Operations = "operations";
    public const string Member = "member";

    /// <summary>
    /// Today any known role on an active membership may use partner write APIs
    /// after retreat-slug authorization. Do not treat this as a privilege check yet.
    /// </summary>
    public static bool IsKnown(string? role)
        => role is Owner or Manager or Finance or Operations or Member;
}
