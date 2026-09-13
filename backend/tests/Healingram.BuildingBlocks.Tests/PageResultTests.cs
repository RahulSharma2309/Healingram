using Healingram.BuildingBlocks.Api;
using Xunit;

namespace Healingram.BuildingBlocks.Tests;

public class PageResultTests
{
    [Fact]
    public void Create_pages_a_collection_without_changing_total()
    {
        var source = Enumerable.Range(1, 45).ToArray();

        var page = PageResult<int>.Create(source, page: 2, pageSize: 20);

        Assert.Equal(2, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(45, page.Total);
        Assert.Equal(Enumerable.Range(21, 20), page.Items);
    }
}
