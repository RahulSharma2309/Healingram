namespace Healingram.Modules.Identity.Wishlist;

internal static class WishlistRules
{
    internal const int MaxSlugLength = 128;

    public static bool IsSignedIn(Guid userId) => userId != Guid.Empty;

    public static IReadOnlyList<string> ValidateSlug(string? slug, out string normalized)
    {
        normalized = slug?.Trim() ?? string.Empty;
        var details = new List<string>();

        if (normalized.Length == 0)
        {
            details.Add("slug is required");
        }
        else if (normalized.Length > MaxSlugLength)
        {
            details.Add("slug is too long");
        }

        return details;
    }
}
