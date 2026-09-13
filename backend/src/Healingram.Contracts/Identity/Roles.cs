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
    public const string AdminRequestsRead = "admin.requests.read";
    public const string AdminRequestsManage = "admin.requests.manage";
    public const string AdminPaymentsSimulate = "admin.payments.simulate";
    public const string AdminUsersRead = "admin.users.read";
    public const string AdminAuditRead = "admin.audit.read";
    public const string AdminLeadsRead = "admin.leads.read";
}
