using System.Net;
using System.Text.Json;
using Healingram.BuildingBlocks.Modules;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Healingram.Api.Tests;

public class HealthAndMetaTests : IClassFixture<HealingramApiFactory>
{
    private static readonly string[] ExpectedModuleNames =
    [
        "Identity", "Catalog", "Matching", "Availability", "Booking", "Payment", "Leads", "Partners"
    ];

    private static readonly string[] ExpectedSchemas =
    [
        "identity", "catalog", "matching", "availability", "booking", "payment", "leads", "partners"
    ];

    private readonly HealingramApiFactory _factory;
    private readonly HttpClient _client;

    public HealthAndMetaTests(HealingramApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_ok_status_version_and_time()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
        Assert.True(DateTimeOffset.TryParse(root.GetProperty("time").GetString(), out var time));
        Assert.True(time <= DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.True(time >= DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Meta_returns_environment_service_and_version()
    {
        var response = await _client.GetAsync("/api/meta");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("Testing", root.GetProperty("environment").GetString());
        Assert.Equal("healingram-api", root.GetProperty("service").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
    }

    [Theory]
    [InlineData("/api/identity/ready", "Identity")]
    [InlineData("/api/catalog/ready", "Catalog")]
    [InlineData("/api/matching/ready", "Matching")]
    [InlineData("/api/availability/ready", "Availability")]
    [InlineData("/api/booking/ready", "Booking")]
    [InlineData("/api/payment/ready", "Payment")]
    [InlineData("/api/leads/ready", "Leads")]
    [InlineData("/api/partners/ready", "Partners")]
    public async Task Module_ready_returns_registered_module_name(string path, string moduleName)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(moduleName, doc.RootElement.GetProperty("module").GetString());
    }

    [Fact]
    public void Host_registers_eight_app_modules()
    {
        using var scope = _factory.Services.CreateScope();
        var modules = scope.ServiceProvider.GetServices<IAppModule>().ToArray();

        Assert.Equal(8, modules.Length);
        Assert.Equal(ExpectedModuleNames, modules.Select(m => m.Name).ToArray());
        Assert.Equal(ExpectedSchemas, modules.Select(m => m.Schema).ToArray());
    }
}
