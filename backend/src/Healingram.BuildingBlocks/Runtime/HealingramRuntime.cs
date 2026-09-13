using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Healingram.BuildingBlocks.Runtime;

public sealed class HealingramRuntime
{
    public const string LocalJwtKey = "healingram-local-dev-jwt-key-change-me-32";
    public const string LocalWebhookSecret = "local-dev-webhook-secret";
    public const string LocalOtpCode = "560142";

    public required bool IsProduction { get; init; }
    public required bool DemoMode { get; init; }
    public required string OtpProvider { get; init; }
    public required string PaymentProvider { get; init; }
    public required string InventoryProvider { get; init; }
    public required string CustomerAppUrl { get; init; }
    public required string VendorAppUrl { get; init; }
    public required string AdminAppUrl { get; init; }
    public required IReadOnlyList<string> CorsOrigins { get; init; }

    public static HealingramRuntime From(IHostEnvironment environment, IConfiguration configuration)
    {
        var customer = configuration["App:CustomerUrl"] ?? "http://localhost:5173";
        var vendor = configuration["App:VendorUrl"] ?? "http://localhost:5173";
        var admin = configuration["App:AdminUrl"] ?? "http://localhost:5173";
        var extra = configuration.GetSection("App:AdditionalCorsOrigins").Get<string[]>() ?? [];
        var configured = extra
            .Concat([customer, vendor, admin])
            .Where(static o => !string.IsNullOrWhiteSpace(o))
            .Select(static o => o.Trim().TrimEnd('/'));
        var origins = environment.IsProduction()
            ? configured.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : configured
                .Concat(["http://localhost:5173", "http://127.0.0.1:5173"])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return new HealingramRuntime
        {
            IsProduction = environment.IsProduction(),
            DemoMode = configuration.GetValue("DemoMode", environment.IsDevelopment()),
            OtpProvider = configuration["Otp:Provider"] ?? "local",
            PaymentProvider = configuration["Payment:Provider"] ?? "local",
            InventoryProvider = configuration["Inventory:Provider"] ?? "local",
            CustomerAppUrl = customer.TrimEnd('/'),
            VendorAppUrl = vendor.TrimEnd('/'),
            AdminAppUrl = admin.TrimEnd('/'),
            CorsOrigins = origins
        };
    }

    public static void EnsureSafeToStart(
        IHostEnvironment environment,
        IConfiguration configuration,
        bool requireApiSecrets = true)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var failures = new List<string>();
        if (configuration.GetValue("Identity:SeedOnStartup", false)
            || configuration.GetValue("Partners:SeedOnStartup", false)
            || configuration.GetValue("DemoMode", false))
        {
            failures.Add("Production cannot seed demo users or run with DemoMode=true");
        }

        if (requireApiSecrets)
        {
            var jwt = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwt) || jwt == LocalJwtKey)
            {
                failures.Add("Jwt:Key must be set to a non-default secret");
            }

            var webhook = configuration["Payment:FakeWebhookSecret"] ?? configuration["Payment:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhook) || webhook == LocalWebhookSecret)
            {
                failures.Add("Payment webhook secret must be set to a non-default value");
            }

            var otp = (configuration["Otp:Provider"] ?? "local").Trim().ToLowerInvariant();
            var payment = (configuration["Payment:Provider"] ?? "local").Trim().ToLowerInvariant();
            var inventory = (configuration["Inventory:Provider"] ?? "local").Trim().ToLowerInvariant();
            if (otp is "local"
                && !configuration.GetValue("Otp:AllowLocalInProduction", false))
            {
                failures.Add("Otp:Provider cannot be local in Production");
            }

            if (payment is "local" or "fake"
                && !configuration.GetValue("Payment:AllowLocalInProduction", false))
            {
                failures.Add("Payment:Provider cannot be local in Production");
            }

            if (inventory is "local"
                && !configuration.GetValue("Inventory:AllowLocalInProduction", false))
            {
                failures.Add("Inventory:Provider cannot be local in Production");
            }

            if (otp is not "local")
            {
                failures.Add($"Otp:Provider '{otp}' is not implemented in this build");
            }

            if (payment is not ("local" or "fake"))
            {
                failures.Add($"Payment:Provider '{payment}' is not implemented in this build");
            }

            if (inventory is not "local")
            {
                failures.Add($"Inventory:Provider '{inventory}' is not implemented in this build");
            }

            if (configuration.GetValue("Payment:AllowLocalSimulate", false))
            {
                failures.Add("Payment:AllowLocalSimulate cannot be true in Production");
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Production refused to start: " + string.Join("; ", failures));
        }
    }
}
