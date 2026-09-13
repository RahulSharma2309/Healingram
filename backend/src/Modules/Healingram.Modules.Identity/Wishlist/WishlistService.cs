using System.Diagnostics;
using Healingram.Modules.Identity.Data;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Identity.Wishlist;

internal sealed record AddWishlistRequest(string? Slug);
internal sealed record WishlistItemResponse(string Slug);
internal sealed record WishlistListResponse(IReadOnlyList<WishlistItemResponse> Items);

internal enum WishlistStatus
{
    Ok,
    Created,
    NoContent,
    Validation,
    Unauthorized
}

internal sealed record WishlistResult(
    WishlistStatus Status,
    WishlistListResponse? List = null,
    string? Error = null,
    IReadOnlyList<string>? Details = null)
{
    public static WishlistResult Listed(WishlistListResponse list) => new(WishlistStatus.Ok, list);
    public static WishlistResult Created(WishlistListResponse list) => new(WishlistStatus.Created, list);
    public static WishlistResult Replay(WishlistListResponse list) => new(WishlistStatus.Ok, list);
    public static WishlistResult Removed() => new(WishlistStatus.NoContent);
    public static WishlistResult Invalid(params string[] details)
        => new(WishlistStatus.Validation, Error: "Validation failed", Details: details);
    public static WishlistResult Rejected()
        => new(WishlistStatus.Unauthorized, Error: "Unauthorized", Details: []);
}

internal sealed class WishlistService(IIdentityStore store, ILogger<WishlistService> logger)
{
    public async Task<WishlistResult> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!WishlistRules.IsSignedIn(userId))
        {
            return WishlistResult.Rejected();
        }

        using var activity = WishlistTelemetry.Source.StartActivity("identity.wishlist.list");
        activity?.SetTag("user.id", userId.ToString());
        return WishlistResult.Listed(await LoadAsync(userId, cancellationToken));
    }

    public async Task<WishlistResult> AddAsync(Guid userId, AddWishlistRequest? request, CancellationToken cancellationToken)
    {
        if (!WishlistRules.IsSignedIn(userId))
        {
            return WishlistResult.Rejected();
        }

        var details = WishlistRules.ValidateSlug(request?.Slug, out var slug);
        if (details.Count > 0)
        {
            return WishlistResult.Invalid([.. details]);
        }

        using var activity = WishlistTelemetry.Source.StartActivity("identity.wishlist.add");
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("wishlist.slug", slug);

        var inserted = await store.TryAddWishlistAsync(userId, slug, cancellationToken);
        logger.LogInformation(
            inserted
                ? "Wishlist added for user {UserId} slug {Slug}"
                : "Wishlist already saved for user {UserId} slug {Slug}",
            userId,
            slug);

        var list = await LoadAsync(userId, cancellationToken);
        return inserted ? WishlistResult.Created(list) : WishlistResult.Replay(list);
    }

    public async Task<WishlistResult> RemoveAsync(Guid userId, string? slug, CancellationToken cancellationToken)
    {
        if (!WishlistRules.IsSignedIn(userId))
        {
            return WishlistResult.Rejected();
        }

        var details = WishlistRules.ValidateSlug(slug, out var normalized);
        if (details.Count > 0)
        {
            return WishlistResult.Invalid([.. details]);
        }

        using var activity = WishlistTelemetry.Source.StartActivity("identity.wishlist.remove");
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("wishlist.slug", normalized);

        await store.RemoveWishlistAsync(userId, normalized, cancellationToken);
        logger.LogInformation("Wishlist removed for user {UserId} slug {Slug}", userId, normalized);
        return WishlistResult.Removed();
    }

    private async Task<WishlistListResponse> LoadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var slugs = await store.ListWishlistSlugsAsync(userId, cancellationToken);
        return new WishlistListResponse(slugs.Select(item => new WishlistItemResponse(item)).ToArray());
    }
}

internal static class WishlistTelemetry
{
    public static readonly ActivitySource Source = new("Healingram.Identity");
}
