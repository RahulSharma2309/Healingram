using System.Reflection;
using Healingram.Modules.Catalog.Domain;
using Xunit;

namespace Healingram.Modules.Catalog.Tests;

public class PublicationGateTests
{
    [Fact]
    public void Published_tamil_nadu_retreat_is_public()
    {
        var input = new PublicationInput(RetreatPublicationStatus.Active, "tamil-nadu", true, 1);
        Assert.True(PublicationGate.IsPubliclyVisible(input));
    }

    [Fact]
    public void Goa_unpublished_is_not_public()
    {
        var input = new PublicationInput(RetreatPublicationStatus.Draft, "goa", true, 2);
        Assert.False(PublicationGate.IsPubliclyVisible(input));
    }

    [Fact]
    public void Goa_published_is_public()
    {
        var input = new PublicationInput(RetreatPublicationStatus.Active, "goa", true, 1);
        Assert.True(PublicationGate.IsPubliclyVisible(input));
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
    public void Incomplete_identity_is_not_public()
    {
        var input = new PublicationInput(RetreatPublicationStatus.Active, "karnataka", false, 1);
        Assert.False(PublicationGate.IsPubliclyVisible(input));
    }

    [Fact]
    public void On_request_price_is_not_shown_as_fact()
    {
        Assert.False(PublicationGate.CanDisplayAsFact(PriceStatus.OnRequest));
        Assert.True(PublicationGate.CanDisplayAsFact(PriceStatus.Verified));
    }

    [Fact]
    public void Publication_gate_has_no_state_allow_list()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        var type = typeof(PublicationGate);
        Assert.Null(type.GetField("V1States", flags));
        Assert.Null(type.GetField("ForbiddenPublicStates", flags));
    }
}
