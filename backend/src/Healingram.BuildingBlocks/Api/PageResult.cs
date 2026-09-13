namespace Healingram.BuildingBlocks.Api;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public static PageResult<T> Create(IReadOnlyList<T> source, int? page, int? pageSize, int defaultSize = 20, int maxSize = 100)
    {
        var safePage = page is null or < 1 ? 1 : page.Value;
        var safeSize = pageSize is null or < 1 ? defaultSize : Math.Min(pageSize.Value, maxSize);
        var total = source.Count;
        var items = source.Skip((safePage - 1) * safeSize).Take(safeSize).ToArray();
        return new PageResult<T>(items, safePage, safeSize, total);
    }
}
