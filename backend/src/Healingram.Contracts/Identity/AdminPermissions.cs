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
    public const string PaymentsManage = "payments.manage";
    public const string PaymentsSimulate = "payments.simulate";
    public const string RefundsManage = "refunds.manage";
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string LeadsRead = "leads.read";
    public const string AuditRead = "audit.read";
    public const string ContentManage = "content.manage";
    public const string SettingsManage = "settings.manage";
    public const string InventoryRead = "inventory.read";
    public const string InventoryManage = "inventory.manage";
    public const string PartnersRead = "partners.read";
    public const string PartnersManage = "partners.manage";

    public static readonly IReadOnlyList<string> All =
    [
        RequestsRead,
        RequestsManage,
        VendorsRead,
        VendorsManage,
        PartnersRead,
        PartnersManage,
        CatalogRead,
        CatalogManage,
        BookingsRead,
        BookingsManage,
        PaymentsRead,
        PaymentsManage,
        PaymentsSimulate,
        RefundsManage,
        UsersRead,
        UsersManage,
        LeadsRead,
        AuditRead,
        ContentManage,
        SettingsManage,
        InventoryRead,
        InventoryManage
    ];
}

public interface IAdminAuthorization
{
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken);
}
