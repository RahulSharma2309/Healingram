using Healingram.BuildingBlocks.Health;
using Xunit;

namespace Healingram.BuildingBlocks.Tests;

public class HealthResponseTests
{
    [Fact]
    public void Health_payload_exposes_status_version_and_time()
    {
        var time = DateTimeOffset.Parse("2026-09-12T12:00:00Z");
        var response = new HealthResponse("ok", "0.1.0", time);

        Assert.Equal("ok", response.Status);
        Assert.Equal("0.1.0", response.Version);
        Assert.Equal(time, response.Time);
    }

    [Fact]
    public void Meta_payload_exposes_environment_service_and_version()
    {
        var response = new MetaResponse("Testing", "healingram-api", "0.1.0");

        Assert.Equal("Testing", response.Environment);
        Assert.Equal("healingram-api", response.Service);
        Assert.Equal("0.1.0", response.Version);
    }
}
