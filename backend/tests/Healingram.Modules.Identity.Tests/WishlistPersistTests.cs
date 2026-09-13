using Healingram.Modules.Identity.Wishlist;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class WishlistPersistTests
{
    [Fact]
    public async Task Guest_is_unauthorized_at_rules_and_maps_to_401()
    {
        Assert.False(WishlistRules.IsSignedIn(Guid.Empty));

        var service = CreateService();
        var list = await service.ListAsync(Guid.Empty, CancellationToken.None);
        var add = await service.AddAsync(Guid.Empty, new AddWishlistRequest("any-slug"), CancellationToken.None);
        var remove = await service.RemoveAsync(Guid.Empty, "any-slug", CancellationToken.None);

        Assert.Equal(WishlistStatus.Unauthorized, list.Status);
        Assert.Equal(WishlistStatus.Unauthorized, add.Status);
        Assert.Equal(WishlistStatus.Unauthorized, remove.Status);

        var http = WishlistEndpoints.WishlistHttp.From(list);
        Assert.Equal(StatusCodes.Status401Unauthorized, ((IStatusCodeHttpResult)http).StatusCode);
    }

    [Fact]
    public async Task Add_then_list_returns_saved_slug()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();

        var created = await service.AddAsync(userId, new AddWishlistRequest("  kairali  "), CancellationToken.None);
        var listed = await service.ListAsync(userId, CancellationToken.None);

        Assert.Equal(WishlistStatus.Created, created.Status);
        Assert.Equal(["kairali"], Slugs(created.List));
        Assert.Equal(["kairali"], Slugs(listed.List));
    }

    [Fact]
    public async Task Duplicate_post_is_idempotent()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();
        var body = new AddWishlistRequest("same-retreat");

        var first = await service.AddAsync(userId, body, CancellationToken.None);
        var second = await service.AddAsync(userId, body, CancellationToken.None);

        Assert.Equal(WishlistStatus.Created, first.Status);
        Assert.Equal(WishlistStatus.Ok, second.Status);
        Assert.Equal(["same-retreat"], Slugs(second.List));
    }

    [Fact]
    public async Task Delete_removes_item()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();
        await service.AddAsync(userId, new AddWishlistRequest("keep-me"), CancellationToken.None);
        await service.AddAsync(userId, new AddWishlistRequest("drop-me"), CancellationToken.None);

        var removed = await service.RemoveAsync(userId, "drop-me", CancellationToken.None);
        var listed = await service.ListAsync(userId, CancellationToken.None);

        Assert.Equal(WishlistStatus.NoContent, removed.Status);
        Assert.Equal(["keep-me"], Slugs(listed.List));
    }

    [Fact]
    public async Task Another_user_does_not_see_the_row()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var service = CreateService();

        await service.AddAsync(owner, new AddWishlistRequest("private-retreat"), CancellationToken.None);
        var otherList = await service.ListAsync(other, CancellationToken.None);

        await service.RemoveAsync(other, "private-retreat", CancellationToken.None);
        var ownerList = await service.ListAsync(owner, CancellationToken.None);

        Assert.Empty(Slugs(otherList.List));
        Assert.Equal(["private-retreat"], Slugs(ownerList.List));
    }

    private static WishlistService CreateService()
        => new(new InMemoryIdentityStore(), NullLogger<WishlistService>.Instance);

    private static IReadOnlyList<string> Slugs(WishlistListResponse? list)
        => list?.Items.Select(item => item.Slug).ToArray() ?? [];
}
