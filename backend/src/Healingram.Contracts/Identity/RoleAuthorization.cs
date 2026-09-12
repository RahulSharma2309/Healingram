using System.Security.Claims;

namespace Healingram.Contracts.Identity;

/// <summary>
/// Server-side role checks. A customer token cannot authorize partner or admin writes.
/// </summary>
public static class RoleAuthorization
{
    public static string? GetRole(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return null;
        }

        return user.FindFirstValue(ClaimTypes.Role)
            ?? user.FindFirstValue("role");
    }

    public static bool SatisfiesPartnerWrite(string? role)
        => IsRole(role, Roles.Partner) || IsRole(role, Roles.Admin);

    public static bool SatisfiesAdminWrite(string? role)
        => IsRole(role, Roles.Admin);

    public static bool CanAuthorizePartnerWrite(ClaimsPrincipal? user)
        => user?.Identity?.IsAuthenticated == true && SatisfiesPartnerWrite(GetRole(user));

    public static bool CanAuthorizeAdminWrite(ClaimsPrincipal? user)
        => user?.Identity?.IsAuthenticated == true && SatisfiesAdminWrite(GetRole(user));

    private static bool IsRole(string? actual, string expected)
        => !string.IsNullOrWhiteSpace(actual)
           && actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
}
