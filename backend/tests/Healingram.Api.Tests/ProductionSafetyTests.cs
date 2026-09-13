using Healingram.BuildingBlocks.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Healingram.Api.Tests;

public class ProductionSafetyTests
{
    [Fact]
    public void Production_refuses_default_jwt_and_demo_mode()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = HealingramRuntime.LocalJwtKey,
            ["Payment:FakeWebhookSecret"] = HealingramRuntime.LocalWebhookSecret,
            ["DemoMode"] = "true",
            ["Otp:Provider"] = "local",
            ["Payment:Provider"] = "local"
        }).Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => HealingramRuntime.EnsureSafeToStart(new StubHost("Production"), config));
        Assert.Contains("Jwt:Key", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_refuses_local_payment_simulation()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "production-jwt-key-must-be-long-enough-32",
            ["Payment:WebhookSecret"] = "production-webhook-secret",
            ["Otp:Provider"] = "local",
            ["Payment:Provider"] = "local",
            ["Otp:AllowLocalInProduction"] = "true",
            ["Payment:AllowLocalInProduction"] = "true",
            ["Inventory:AllowLocalInProduction"] = "true",
            ["Payment:AllowLocalSimulate"] = "true",
            ["DemoMode"] = "false"
        }).Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => HealingramRuntime.EnsureSafeToStart(new StubHost("Production"), config));
        Assert.Contains("AllowLocalSimulate", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Development_allows_local_defaults()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = HealingramRuntime.LocalJwtKey
        }).Build();

        HealingramRuntime.EnsureSafeToStart(new StubHost("Development"), config);
    }

    [Fact]
    public void Production_refuses_demo_seeds_even_with_custom_secrets()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "production-jwt-key-must-be-long-enough-32",
            ["Payment:WebhookSecret"] = "production-webhook-secret",
            ["Otp:Provider"] = "local",
            ["Payment:Provider"] = "local",
            ["Otp:AllowLocalInProduction"] = "true",
            ["Payment:AllowLocalInProduction"] = "true",
            ["Inventory:AllowLocalInProduction"] = "true",
            ["Identity:SeedOnStartup"] = "true",
            ["DemoMode"] = "false"
        }).Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => HealingramRuntime.EnsureSafeToStart(new StubHost("Production"), config));
        Assert.Contains("seed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_refuses_unimplemented_providers()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "production-jwt-key-must-be-long-enough-32",
            ["Payment:WebhookSecret"] = "production-webhook-secret",
            ["Otp:Provider"] = "twilio",
            ["Payment:Provider"] = "razorpay",
            ["Inventory:Provider"] = "external",
            ["DemoMode"] = "false"
        }).Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => HealingramRuntime.EnsureSafeToStart(new StubHost("Production"), config));
        Assert.Contains("not implemented", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_cors_excludes_localhost()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["App:CustomerUrl"] = "https://healingram.com",
            ["App:VendorUrl"] = "https://vendor.healingram.com",
            ["App:AdminUrl"] = "https://admin.healingram.com"
        }).Build();

        var runtime = HealingramRuntime.From(new StubHost("Production"), config);
        Assert.DoesNotContain(runtime.CorsOrigins, origin => origin.Contains("localhost", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("https://healingram.com", runtime.CorsOrigins);
        Assert.Contains("https://vendor.healingram.com", runtime.CorsOrigins);
    }

    [Fact]
    public void Development_cors_includes_localhost()
    {
        var runtime = HealingramRuntime.From(new StubHost("Development"), new ConfigurationBuilder().Build());
        Assert.Contains("http://localhost:5173", runtime.CorsOrigins);
    }

    private sealed class StubHost(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
