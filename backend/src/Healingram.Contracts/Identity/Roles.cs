namespace Healingram.Contracts.Identity;

public static class Roles
{
    public const string Customer = "customer";
    public const string Partner = "partner";
    public const string Admin = "admin";
}

public static class IdentityPolicies
{
    public const string PartnerWrite = "PartnerWrite";
    public const string AdminWrite = "AdminWrite";
}
