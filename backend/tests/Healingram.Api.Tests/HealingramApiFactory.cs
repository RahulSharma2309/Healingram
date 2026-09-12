using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Healingram.Api.Tests;

public sealed class HealingramApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Schema:ApplyOnStartup"] = "false",
                ["Seq:Url"] = "",
                ["OpenTelemetry:OtlpEndpoint"] = ""
            });
        });
    }
}
