using System.Net;
using System.Text.Json;
using Healingram.BuildingBlocks.Observability;
using Xunit;

namespace Healingram.Gateway.Tests;

public class CorrelationIdGatewayTests : IClassFixture<HealingramGatewayFactory>
{
    private readonly HttpClient _client;

    public CorrelationIdGatewayTests(HealingramGatewayFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/gateway/health")]
    [InlineData("/api/health")]
    public async Task Correlation_id_is_created_when_client_sends_none(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var id = GetCorrelationId(response);
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.Equal(32, id.Length);
    }

    [Theory]
    [InlineData("/api/gateway/health")]
    [InlineData("/api/health")]
    public async Task Correlation_id_is_echoed_when_client_sends_one(string path)
    {
        const string incoming = "client-corr-42";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, incoming);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(incoming, GetCorrelationId(response));
    }

    [Fact]
    public async Task Proxied_request_forwards_the_same_correlation_id_to_the_api()
    {
        const string incoming = "forward-me-please";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, incoming);

        var response = await _client.SendAsync(request);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(incoming, GetCorrelationId(response));
        Assert.Equal(incoming, doc.RootElement.GetProperty("forwardedCorrelationId").GetString());
    }

    [Fact]
    public async Task Proxied_response_keeps_gateway_correlation_id_when_api_omits_the_header()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(GetCorrelationId(response)));
    }

    private static string GetCorrelationId(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(CorrelationId.HeaderName, out var values))
        {
            return values.Single();
        }

        if (response.Content.Headers.TryGetValues(CorrelationId.HeaderName, out values))
        {
            return values.Single();
        }

        return string.Empty;
    }
}
