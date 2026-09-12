using Healingram.Modules.Catalog.Domain;
using Xunit;

namespace Healingram.Modules.Catalog.Tests;

public class PublicationGateTests
{
    [Fact]
    public void Active_karnataka_retreat_with_programme_is_public()
    {
        var input = new PublicationInput(RetreatPublicationStatus.Active, "karnataka", true, 1);
        Assert.True(PublicationGate.IsPubliclyVisible(input));
    }

    [Fact]
    public void Goa_is_never_public()
    {
        var input = new PublicationInput(RetreatPublicationStatus.Active, "goa", true, 2);
        Assert.False(PublicationGate.IsPubliclyVisible(input));
    }

    [Fact]
    public void Draft_is_not_public()
    {
        var input = new PublicationInput(RetreatPublicationStatus.Draft, "kerala", true, 1);
        Assert.False(PublicationGate.IsPubliclyVisible(input));
    }

    [Fact]
    public void Missing_programme_is_not_public()
    {
        var input = new PublicationInput(RetreatPublicationStatus.Active, "kerala", true, 0);
        Assert.False(PublicationGate.IsPubliclyVisible(input));
    }

    [Fact]
    public void On_request_price_is_not_shown_as_fact()
    {
        Assert.False(PublicationGate.CanDisplayAsFact(PriceStatus.OnRequest));
        Assert.True(PublicationGate.CanDisplayAsFact(PriceStatus.Verified));
    }
}
