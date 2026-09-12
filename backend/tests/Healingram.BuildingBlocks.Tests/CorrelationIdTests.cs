using Healingram.BuildingBlocks.Observability;
using Xunit;

namespace Healingram.BuildingBlocks.Tests;

public class CorrelationIdTests
{
    [Fact]
    public void Ensure_generates_when_missing()
    {
        var id = CorrelationId.Ensure(null);
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.Equal(32, id.Length);
    }

    [Fact]
    public void Ensure_keeps_incoming_header()
    {
        Assert.Equal("abc-123", CorrelationId.Ensure("abc-123"));
    }

    [Fact]
    public void Ensure_rejects_oversized_header()
    {
        var huge = new string('a', 80);
        var id = CorrelationId.Ensure(huge);
        Assert.NotEqual(huge, id);
        Assert.Equal(32, id.Length);
    }
}
