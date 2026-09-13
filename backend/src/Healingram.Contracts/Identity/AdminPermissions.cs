namespace Healingram.Contracts.Identity;

public static class AdminPermissions
{
    public const string RequestsRead = "requests.read";
    public const string RequestsManage = "requests.manage";
    public const string VendorsRead = "vendors.read";
    public const string VendorsManage = "vendors.manage";
    public const string CatalogRead = "catalog.read";
    public const string CatalogManage = "catalog.manage";
    public const string BookingsRead = "bookings.read";
    public const string BookingsManage = "bookings.manage";
    public const string PaymentsRead = "payments.read";
    public const string RefundsManage = "refunds.manage";
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string AuditRead = "audit.read";

    public static readonly IReadOnlyList<string> All =
    [
        RequestsRead,
        RequestsManage,
        VendorsRead,
        VendorsManage,
        CatalogRead,
        CatalogManage,
        BookingsRead,
        BookingsManage,
        PaymentsRead,
        RefundsManage,
        UsersRead,
        UsersManage,
        AuditRead
    ];
}

public interface IAdminAuthorization
{
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken);
}
