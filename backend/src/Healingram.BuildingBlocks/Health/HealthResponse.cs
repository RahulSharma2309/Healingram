namespace Healingram.BuildingBlocks.Health;

public sealed record HealthResponse(string Status, string Version, DateTimeOffset Time);

public sealed record MetaResponse(string Environment, string Service, string Version);
