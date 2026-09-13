using System.Net;
using System.Text.Json;
using Xunit;

namespace Healingram.Gateway.Tests;

public class GatewayProxyTests : IClassFixture<HealingramGatewayFactory>
{
    private readonly HttpClient _client;

    public GatewayProxyTests(HealingramGatewayFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Browser_origin_receives_cors_headers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/gateway/health");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "http://localhost:5173",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Gateway_health_is_local_and_does_not_need_the_api()
    {
        var response = await _client.GetAsync("/api/gateway/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("gateway", doc.RootElement.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Health_is_proxied_to_the_api()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("0.1.0", doc.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Meta_is_proxied_to_the_api()
    {
        var response = await _client.GetAsync("/api/meta");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Testing", doc.RootElement.GetProperty("environment").GetString());
        Assert.Equal("healingram-api", doc.RootElement.GetProperty("service").GetString());
    }

    [Theory]
    [InlineData("/openapi")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/api/docs")]
    public async Task OpenApi_is_available_through_the_gateway(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("3.0.1", doc.RootElement.GetProperty("openapi").GetString());
        Assert.Equal("Healingram", doc.RootElement.GetProperty("info").GetProperty("title").GetString());
    }
}
