using System.Security.Claims;

namespace Healingram.Contracts.Identity;

/// <summary>
/// Server-side role checks. A customer token cannot authorize partner or admin writes.
/// </summary>
public static class RoleAuthorization
{
    public static string? GetRole(ClaimsPrincipal? user)
        => PrimaryRole(GetRoles(user));

    public static IReadOnlyList<string> GetRoles(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return [];
        }

        return NormalizeRoles(
            user.FindAll(ClaimTypes.Role)
                .Concat(user.FindAll("role"))
                .Select(c => c.Value));
    }

    public static IReadOnlyList<string> NormalizeRoles(IEnumerable<string>? roles, string? fallback = null)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (roles is not null)
        {
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    set.Add(role.Trim().ToLowerInvariant());
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            set.Add(fallback.Trim().ToLowerInvariant());
        }

        var ordered = new List<string>();
        foreach (var role in new[] { Roles.Admin, Roles.Partner, Roles.Customer })
        {
            if (set.Remove(role))
            {
                ordered.Add(role);
            }
        }

        ordered.AddRange(set.OrderBy(static r => r, StringComparer.Ordinal));
        return ordered;
    }

    public static string PrimaryRole(IEnumerable<string>? roles, string? fallback = null)
        => NormalizeRoles(roles, fallback).FirstOrDefault() ?? Roles.Customer;

    public static bool SatisfiesPartnerWrite(string? role)
        => IsRole(role, Roles.Partner) || IsRole(role, Roles.Admin);

    public static bool SatisfiesPartnerWrite(IEnumerable<string>? roles)
        => roles is not null && roles.Any(SatisfiesPartnerWrite);

    public static bool SatisfiesAdminWrite(string? role)
        => IsRole(role, Roles.Admin);

    public static bool SatisfiesAdminWrite(IEnumerable<string>? roles)
        => roles is not null && roles.Any(SatisfiesAdminWrite);

    public static bool CanAuthorizePartnerWrite(ClaimsPrincipal? user)
        => user?.Identity?.IsAuthenticated == true && SatisfiesPartnerWrite(GetRoles(user));

    public static bool CanAuthorizeAdminWrite(ClaimsPrincipal? user)
        => user?.Identity?.IsAuthenticated == true && SatisfiesAdminWrite(GetRoles(user));

    public static string? GetPurpose(ClaimsPrincipal? user)
        => user?.FindFirstValue("purpose");

    public static string? GetScopedRequestId(ClaimsPrincipal? user)
        => user?.FindFirstValue("request_id");

    public static string? GetAuthKind(ClaimsPrincipal? user)
        => user?.FindFirstValue(AuthKinds.Claim);

    private static bool IsRole(string? actual, string expected)
        => !string.IsNullOrWhiteSpace(actual)
           && actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
}
