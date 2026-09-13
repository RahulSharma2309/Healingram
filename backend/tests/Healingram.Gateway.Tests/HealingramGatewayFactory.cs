using Healingram.BuildingBlocks.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Healingram.Gateway.Tests;

public sealed class HealingramGatewayFactory : WebApplicationFactory<Program>
{
    private WebApplication? _downstream;
    private string _downstreamAddress = "http://127.0.0.1:0";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        StartDownstream();

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seq:Url"] = "",
                ["OpenTelemetry:OtlpEndpoint"] = "",
                ["ReverseProxy:Clusters:monolith:Destinations:api:Address"] = _downstreamAddress
            });
        });
    }

    private void StartDownstream()
    {
        if (_downstream is not null)
        {
            return;
        }

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();
        app.MapGet("/api/health", (HttpContext context) =>
        {
            var forwarded = context.Request.Headers[CorrelationId.HeaderName].ToString();
            return Results.Ok(new
            {
                status = "ok",
                version = "0.1.0",
                time = DateTimeOffset.UtcNow,
                forwardedCorrelationId = forwarded
            });
        });
        app.MapGet("/api/meta", () => Results.Ok(new
        {
            environment = "Testing",
            service = "healingram-api",
            version = "0.1.0"
        }));
        app.MapGet("/openapi/v1.json", () => Results.Json(new
        {
            openapi = "3.0.1",
            info = new { title = "Healingram", version = "0.1.0" }
        }));

        app.StartAsync().GetAwaiter().GetResult();
        _downstream = app;
        _downstreamAddress = app.Urls.First();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_downstream is not null)
        {
            await _downstream.StopAsync();
            await _downstream.DisposeAsync();
            _downstream = null;
        }

        await base.DisposeAsync();
    }
}
