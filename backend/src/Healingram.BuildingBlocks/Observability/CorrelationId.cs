namespace Healingram.BuildingBlocks.Observability;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public static string Ensure(string? incoming)
    {
        if (!string.IsNullOrWhiteSpace(incoming) && incoming.Length <= 64)
        {
            return incoming.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
